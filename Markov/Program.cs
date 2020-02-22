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

            var l = GetFeed().Result;
            var nS = l[0].GetDefaultNamespace();
            var items = l.Select(x => new
            {
                Headline = x.Element(nS + "title").Value,
                GUID = x.Element(nS + "guid").Value,
                Date = DateTime.Parse(x.Element(nS + "pubDate").Value)
            })
                .ToList();

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
_UpdateHeadlines();
            ParseHelper.GetWords();
        }

        static async Task<List<XElement>> GetFeed()
        {
            List<string> endpoints = new List<string>()
            {
                "articles.rss", "home/index.rss", "news/index.rss", "health/index.rss", "sciencetech/index.rss",
                "news/articles.rss"
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

        //Workaround for existing data
        static void _UpdateHeadlines()
        {
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();

                comm.CommandText = "SELECT Headline FROM Headlines";
                DataTable dT = new DataTable();
                dT.Load(comm.ExecuteReader());

                Dictionary<string, string> keys = new Dictionary<string, string>(){{"Headline", ""}};
                Dictionary<string, string> fields = new Dictionary<string, string>(){{"Sanitised Headline", ""}};

                foreach (var row in dT.AsEnumerable())
                {
                    var head = row[0] as string;
                    keys["Headline"] = head;
                    fields["Sanitised Headline"] = ParseHelper.SanitiseHeadline(head);
                    SQLiteWrapper.UpdateRecord(conn, "Headlines", keys, fields);
                }
            }
        }

    }

}
