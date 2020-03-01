using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Markov
{
    class Program
    {
        static void Main(string[] args)
        {
            SQLiteWrapper.CheckDatabase();

            Train.RandomTrain();
            return;

            SpoolSentence.GenerateSentence(SpoolSentence.GetFOLinks().ToList(), SpoolSentence.GetSOLinks().ToList());

            Console.WriteLine("Retrieving feeds...");

            var l = GetFeed().Result;
            var nS = l[0].GetDefaultNamespace();
            var items = l.Select(x => new
            {
                Headline = x.Element(nS + "title").Value,
                GUID = x.Element(nS + "guid").Value,
                Date = DateTime.Parse(x.Element(nS + "pubDate").Value)
            })
                .ToList();

            Console.WriteLine("Got Feeds");

            DateTime maxDateTime;

            using (SQLiteConnection conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = "SELECT IFNULL(MAX(DATETIME(Date)), DATETIME('2020-01-01 01:00:00.000')) FROM Headlines";
                maxDateTime = DateTime.Parse(comm.ExecuteScalar() as string);
            }

            List<Dictionary<string, string>> colVals = new List<Dictionary<string, string>>();

            //foreach distinct guid where it's newer than most recent record
            foreach (var item in items.GroupBy(x => x.GUID).Select(x => x.First()).Where(x => DateTime.Compare(maxDateTime, x.Date) < 0).ToList())
            {
                colVals.Add(new Dictionary<string, string>()
                {
                    {"Headline", item.Headline},
                    {"GUID", item.GUID},
                    {"Date", item.Date.ToString("yyyy-MM-dd HH:mm:ss.fff")},
                    {"Sanitised Headline", ParseHelper.SanitiseHeadline(item.Headline)}
                });
            }

            SQLiteWrapper.InsertRecords("Headlines", colVals);

            Console.WriteLine("Finished updating headline list");
            Console.WriteLine("Calculating word relations...");

            ParseHelper.CalculateWordRelations();

            Console.WriteLine("Word relations calculated!");
            Console.WriteLine("Generating sentence...");

            SpoolSentence.GenerateSentence(SpoolSentence.GetFOLinks().ToList(), SpoolSentence.GetSOLinks().ToList());
        }

        static async Task<List<XElement>> GetFeed()
        {
            List<string> endpoints = new List<string>()
            {
                "articles.rss", "home/index.rss", "news/index.rss", "health/index.rss", "sciencetech/index.rss",
                "news/articles.rss", "ushome/index.rss"
            };

            List<XElement> elements = new List<XElement>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://www.dailymail.co.uk/");
                foreach (var end in endpoints)
                {
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, end);
                    var resp = await client.SendAsync(req);
                    var el = XElement.Parse(await resp.Content.ReadAsStringAsync());
                    var nS = el.GetDefaultNamespace();
                    elements.AddRange(el.Element(nS + "channel").Elements(nS + "item"));
                }
            }

            return elements;
        }

    }

}
