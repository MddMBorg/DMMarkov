using System;
using System.Collections.Generic;
using System.Linq;

namespace Markov
{
    public class Train
    {
        public delegate double wD(string next, params string[] words);

        private static bool _FirstWeightsCalculated = false;

        public static Dictionary<wD, double> _Funcs = new Dictionary<wD, double>()
            {
                { Weighting.FavourAlliteration, 0.3},
                { Weighting.FavourRarity, 0.3},
                { Weighting.FavourCommonness, 0.3},
                { Weighting.FavourAlternatingCommonness, 0.3},
                { Weighting.FavourDifferentLengths, 0.3},
                { Weighting.FavourSameLengths, 0.3},
                { Weighting.FavourNextPlural, 0.3},
                { Weighting.FavourVowels, 0.3},
                { Weighting.FavourConsonants, 0.3},
                { Weighting.FavourTotalSameness, 0.3},
                { Weighting.FavourTotalUniqueness, 0.3}
            };

        public static void RandomTrain()
        {
            Random rand = new Random();

            var words = ParseHelper.GetWordList();
            Weighting.WordInstances = words.SelectMany(x => x.Select(y => y)).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            double maxOccur = (double)Weighting.WordInstances.Max(x => x.Value);
            Weighting.RelativeOccurence = Weighting.WordInstances.ToDictionary(x => x.Key, x => x.Value / maxOccur);

            List<FOMarkov> firsts = new List<FOMarkov>();
            List<SOMarkov> seconds = new List<SOMarkov>();

            ParseHelper.GetLinks(words, firsts, seconds);

            var sortedFirsts = new SortedList<string, List<FOMarkov>>(firsts.GroupBy(x => x.Base).ToDictionary(x => x.Key, x => x.ToList()));
            int testLoops = 20;
            int divisions = words.Count / testLoops;
            DateTime start = DateTime.Now;

            int mostAccurateFOConfig = 0;
            double mostAccurateFOValue = 0;
            //833, 0.193
            for (int change = 0; change < 100000; change++)
            {
                _Funcs[Weighting.FavourAlternatingCommonness] = (double)change % 10;
                _Funcs[Weighting.FavourTotalUniqueness] = (double)(change / 10) % 10;
                _Funcs[Weighting.FavourDifferentLengths] = (double)(change / 100) % 10;
                _Funcs[Weighting.FavourNextPlural] = (double)(change / 1000) % 10;
                _Funcs[Weighting.FavourVowels] = (double)(change / 10000) % 10;

                LearnFirstWeights(sortedFirsts, firsts, 0.4);

                double avgFOaccuracy = 0;
                for (int i = 0; i < testLoops; i++)
                {
                    var headlineToTest = words[i * divisions + rand.Next(divisions)];
                    var generated = GenerateFOSentence(headlineToTest[0], sortedFirsts, headlineToTest.Count).ToList();
                    int maxWords = generated.Count();
                    int compWords = 0;
                    for (int j = 0; j < maxWords; j++)
                    {
                        compWords += headlineToTest[j] == generated[j] ? 1 : 0;
                    }
                    avgFOaccuracy += (double)compWords / maxWords;
                }
                avgFOaccuracy /= testLoops;

                if (change % 50 == 0)
                {
                    Console.WriteLine($"Config = {change}");
                    Console.WriteLine($"Average FO accuracy = {avgFOaccuracy}");
                }
                if (change % 1000 == 0)
                {
                    var numSeconds = DateTime.Now.Subtract(start).TotalSeconds;
                    Console.WriteLine($"Time since start: {numSeconds} seconds");
                    Console.WriteLine($"Avg time per test: {numSeconds / change} seconds");
                }
                if (avgFOaccuracy > mostAccurateFOValue)
                {
                    mostAccurateFOValue = avgFOaccuracy;
                    mostAccurateFOConfig = change;
                }
            }
        }

        public static void LearnFirstWeights(SortedList<string, List<FOMarkov>> firstsSorted, List<FOMarkov> firsts, double bias)
        {
            //setup list initially
            if (!_FirstWeightsCalculated)
            {
                CalculateFirstWeights(firsts);
                _FirstWeightsCalculated = true;
            }

            List<double> funcsWeights = _Funcs.Values.ToList();
            int max = funcsWeights.Count;

            foreach (var pair in firsts)
            {
                pair.Weight = bias;
                for (int i = 0; i < max; i++)
                {
                    pair.Weight += pair.Weights[i] * funcsWeights[i];
                }
            }

            foreach (var g in firstsSorted.Values)
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }
        }

        public static void CalculateFirstWeights(List<FOMarkov> firsts)
        {
            foreach (var pair in firsts)
            {
                var temp = new List<double>();
                foreach (var func in _Funcs)
                {
                    temp.Add(func.Key(pair.Next, pair.Base));
                }
                pair.Weights = temp;
            }
        }

        public static void LearnWeights(List<List<string>> headlines, List<FOMarkov> firsts, List<SOMarkov> seconds, double bias)
        {
            //LearnFirstWeights(headlines, firsts, bias);
        }

        public static IEnumerable<string> GenerateFOSentence(string firstWord, SortedList<string, List<FOMarkov>> firsts, int maxLength)
        {
            Random rand = new Random();

            string currentword = firstWord;
            yield return currentword;
            for (int i = 1; i < maxLength; i++)
            {
                double p = rand.NextDouble();
                if (!firsts.TryGetValue(currentword, out var poss))
                    break;

                int j = 0;
                while (p > 0.0)
                {
                    currentword = poss[j].Next;
                    p -= poss[j].Probability;
                    j++;
                }
                yield return currentword;
            }
        }

        public static void PrintTrainedSentences(string firstWord, SortedList<string, List<FOMarkov>> sortedFirsts, int maxLength) =>
            Console.WriteLine(GenerateFOSentence(firstWord, sortedFirsts, maxLength));

    }

}
