using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Markov
{
    public class SpoolSentence
    {

        public static void GenerateSentence()
        {
            var firsts = GetFOLinks().ToList();
            var seconds = GetSOLinks().ToList();
            var rand = new Random();
            string sentence = "";

            string currentword = firsts[rand.Next(firsts.Count)].Base;
            sentence = currentword;
            for (int i = 0; i < 20; i++)
            {
                double p = rand.NextDouble();
                var poss = firsts.Where(x => x.Base == currentword).ToList();
                if (poss.Count == 0)
                    break;

                int j = 0;
                while (p > 0.0)
                {
                    currentword = poss[j].Next;
                    p -= poss[j].Probability;
                    j++;
                }
                sentence += " " + new string(currentword.Select(x => x == '_' ? (char)(rand.Next(1, 9) + '0') : x).ToArray());
            }
            Console.WriteLine(sentence);
            GenerateSentence2();
        }

        static void GenerateSentence2()
        {
            var seconds = GetSOLinks().ToList();
            var rand = new Random();
            string sentence = "";
            var potSec = seconds.Where(x => x.Probability != 1).ToList();

            string currentword = potSec[rand.Next(potSec.Count)].Base;
            sentence = currentword;
            potSec = seconds.Where(x => x.Base == currentword && x.Probability != 1).ToList();
            string secondWord = potSec[rand.Next(potSec.Count)].Base2;
            sentence += " " + secondWord;

            for (int i = 0; i < 20; i++)
            {
                double p = rand.NextDouble();
                var poss = seconds.Where(x => x.Base == currentword && x.Base2 == secondWord).ToList();
                if (poss.Count == 0)
                    break;

                currentword = secondWord;
                int j = 0;
                while (p > 0.0)
                {
                    secondWord = poss[j].Next;
                    p -= poss[j].Probability;
                    j++;
                }
                sentence += " " + new string(secondWord.Select(x => x == '_' ? (char)(rand.Next(1, 9) + '0') : x).ToArray());
            }
            Console.WriteLine(sentence);
        }

        static IEnumerable<FOMarkov> GetFOLinks()
        {
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = "SELECT Word, Subsequent, Probability FROM MarkovProbabilities;";
                using (var reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new FOMarkov() { Base = (string)reader.GetValue(0), Next = (string)reader.GetValue(1), Probability = (double)reader.GetValue(2) };
                    }
                }
            }
        }

        static IEnumerable<SOMarkov> GetSOLinks()
        {
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = "SELECT Word1, Word2, Subsequent, Probability FROM Markov2Probabilities;";
                using (var reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new SOMarkov() { Base = (string)reader.GetValue(0), Base2 = (string)reader.GetValue(1), Next = (string)reader.GetValue(2), Probability = (double)reader.GetValue(3) };
                    }
                }
            }
        }

    }

}