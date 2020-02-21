using System;
using System.Collections.Generic;
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

            var xEl = GetFeed().Result;
            var nS = xEl.GetDefaultNamespace();
            var l = xEl.Element(nS + "channel").Elements(nS + "item");
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
                comm.CommandText = "SELECT IFNULL(MAX(DATETIME('Date')), DATETIME('2020-01-01 01:00:00.000')) FROM Headlines";
                maxDateTime = DateTime.Parse(comm.ExecuteScalar() as string);
            }

            List<Dictionary<string, string>> colVals = new List<Dictionary<string, string>>();
            foreach (var item in items)
            {
                if (DateTime.Compare(maxDateTime, item.Date) < 0)
                {
                    colVals.Add(new Dictionary<string, string>()
                    {
                        {"Headline", item.Headline},
                        {"GUID", item.GUID},
                        {"Date", item.Date.ToString("yyyy-MM-dd hh:mm:ss.fff")}
                    });
                }
            }

            SQLiteWrapper.InsertRecords("Headlines", colVals);
        }

        static async Task<XElement> GetFeed()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://www.dailymail.co.uk/");
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, "articles.rss");
                var resp = await client.SendAsync(req);
                return XElement.Parse(await resp.Content.ReadAsStringAsync());
            }
        }


    }

}
