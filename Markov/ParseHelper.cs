using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Markov
{
    public class ParseHelper
    {
        private static readonly List<char> _Currencies = new List<char>() {'$', '€', '£'};

        public static async void GetWords()
        {
            var heads = await _RetrieveHeadlines();
            List<string> words = new List<string>();
            foreach (var head in heads)
            {
                var wordList = head.Split(' ').SelectMany(x => _SanitiseHeadlineWord(x)).Where(x => !string.IsNullOrEmpty(x));
                foreach (var word in wordList)
                words.Add(word);
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

        static IEnumerable<string> _SanitiseHeadlineWord(string headlineWord)
        {
            string ret = headlineWord
                .Replace(":", "")
                .Replace(",", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Trim('\'')
                .Trim('"')
                .Trim('.')
                .Trim('-')
                .Trim('`')
                .Trim(';')
                .ToLower();

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