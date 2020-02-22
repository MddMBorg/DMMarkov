using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Markov
{
    public class ParseHelper
    {
        private static readonly List<char> _Currencies = new List<char>() { '$', '€', '£' };
        private static readonly char[] _TrimChars = new char[]{'\'', '.', '-'};

        private class Relation
        {
            public string Base;
            public string Next;
            public int Count = 0;
            public int ShareSentence = 0;
        }

        public static async void GetWords()
        {
            var heads = await _RetrieveHeadlines();

            List<Relation> links = new List<Relation>();

            List<string> words = new List<string>();

            string curr = "";
            string next = "";
            foreach (var head in heads)
            {
                var wordList = head.Split(' ').SelectMany(x => _SanitiseHeadlineWord(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();
                int total = wordList.Count();
                for (int i = 0; i < total - 1; i++)
                {
                    curr = wordList[i];
                    next = wordList[i + 1];

                    var t = links.FirstOrDefault(x => x.Base == curr && x.Next == next);
                    if (t != null)
                        t.Count++;
                    else
                        links.Add(new Relation() { Base = curr, Next = next, Count = 1 });

                    for (int j = i + 1; j < total; j++)
                    {
                        next = wordList[j];
                        t = links.FirstOrDefault(x => x.Base == curr && x.Next == next);
                        if (t != null)
                            t.ShareSentence++;
                        else
                            links.Add(new Relation() { Base = curr, Next = next, ShareSentence = 1 });
                    }
                }
            }
        }

        static async Task<List<string>> _RetrieveHeadlines()
        {
            List<string> ret = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = "SELECT Headline FROM Headlines";
                var reader = await comm.ExecuteReaderAsync();
                while (reader.Read())
                {
                    ret.Add(reader.GetValue(0) as string);
                }
            }

            return ret;
        }

        public static string SanitiseHeadline(string headline)
        {
            return headline
                .Replace(":", "")
                .Replace(",", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(";", "")
                .Replace("`", "")
                .Replace("£", "$")
                .Replace("€", "$")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\"", "")
                .ToLower();
        }

        static IEnumerable<string> _SanitiseHeadlineWord(string headlineWord)
        {
            string ret = headlineWord
                .Trim(_TrimChars);

            int i = 0;
            string yRet = "";
            bool parsingNumber = false;
            char curr;

            while (i < ret.Length)
            {
                curr = ret[i];
                if (_Currencies.Contains(curr))
                {
                    yield return yRet;
                    yield return "$";
                }
                else if (curr >= '0' && curr <= '9')
                {
                    if (!parsingNumber)
                    {
                        yield return yRet;
                        yRet = "";
                        parsingNumber = true;
                    }
                    yRet += '`';
                }
                else if (parsingNumber)
                {
                    if (curr == '.')
                        yRet += curr;
                    else
                    {
                        yield return yRet;
                        yRet = curr.ToString();
                        parsingNumber = false;
                    }
                }
                else
                {
                    yRet += curr;
                }
                i++;
            }
            yield return yRet;
        }

    }

}