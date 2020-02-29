using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Markov
{
    public class ParseHelper
    {
        private static readonly List<char> _Currencies = new List<char>() { '$', '€', '£' };
        private static readonly char[] _TrimChars = new char[] { '\'', '.', '-' };

        public static async void CalculateWordRelations()
        {
            var heads = await _RetrieveHeadlines();

            List<FOMarkov> links = new List<FOMarkov>();
            List<SOMarkov> links2 = new List<SOMarkov>();

            List<string> words = new List<string>();

            string prev = "";
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
                        t.Count += 1;
                    else
                        links.Add(new FOMarkov() 
                        { Base = curr, Next = next,
                         Weight = 0.3 + 0.6 * Weighting.WeightingSample(next, curr), Count = 1 });

                    if (i > 0)
                    {
                        prev = wordList[i - 1];

                        var t2 = links2.FirstOrDefault(x => x.Base == prev && x.Base2 == curr && x.Next == next);
                        if (t2 != null)
                            t2.Count += 1;
                        else
                            links2.Add(new SOMarkov() 
                            { Base = prev, Base2 = curr, Next = next,
                             Weight = 0.3 + 0.6 * Weighting.WeightingSample(next, prev, curr), Count = 1 });
                    }
                }
            }

            foreach (var g in links.GroupBy(x => x.Base))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }

            foreach (var g in links2.GroupBy(x => new { x.Base, x.Base2 }))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }

            _UpdateLinks1(links);
            _UpdateLinks2(links2);
        }

        static async Task<List<string>> _RetrieveHeadlines()
        {
            List<string> ret = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = "SELECT `Sanitised Headline` FROM Headlines";
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
                    yRet += '_';
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

        static void _UpdateLinks1(List<FOMarkov> links)
        {
            //batch insert to significantly decrease time c. 100x with max 1000 parameters, or even further if using non-parameters (unsafe)
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                string valStr = "";

                for (int i = 0; i <= (links.Count - 1) / 300; i++)
                {
                    valStr = "";
                    comm.Parameters.Clear();
                    int max = Math.Min(links.Count - i * 300, 300);

                    for (int j = 0; j < max; j++)
                    {
                        var l = links[i * 300 + j];
                        valStr += $"($W{j}, $S{j}, $P{j}),";
                        comm.Parameters.AddWithValue($"$W{j}", l.Base);
                        comm.Parameters.AddWithValue($"$S{j}", l.Next);
                        comm.Parameters.AddWithValue($"$P{j}", l.Probability);
                    }
                    valStr = valStr.Trim(',');
                    comm.CommandText = $@"INSERT OR REPLACE INTO MarkovProbabilities(Word, Subsequent, Probability)
                                        VALUES {valStr};";

                    comm.ExecuteNonQuery();
                }
            }
        }

        static void _UpdateLinks2(List<SOMarkov> links)
        {
            //batch insert to significantly decrease time c. 100x with max 1000 parameters, or even further if using non-parameters (unsafe)
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                string valStr = "";

                for (int i = 0; i <= links.Count / 220; i++)
                {
                    valStr = "";
                    comm.Parameters.Clear();
                    int max = Math.Min(links.Count - i * 220, 220);

                    for (int j = 0; j < max; j++)
                    {
                        var l = links[i * 220 + j];
                        valStr += $"($W{j}, $2W{j}, $S{j}, $P{j}),";
                        comm.Parameters.AddWithValue($"$W{j}", l.Base);
                        comm.Parameters.AddWithValue($"$2W{j}", l.Base2);
                        comm.Parameters.AddWithValue($"$S{j}", l.Next);
                        comm.Parameters.AddWithValue($"$P{j}", l.Probability);
                    }
                    valStr = valStr.Trim(',');
                    comm.CommandText = $@"INSERT OR REPLACE INTO Markov2Probabilities(Word1, Word2, Subsequent, Probability)
                                        VALUES {valStr};";

                    comm.ExecuteNonQuery();
                }
            }
        }

    }

}