using System;
using System.Collections.Generic;
using System.Linq;

namespace Markov
{
    public class Train
    {
        public delegate double wD(string next, params string[] words);

        static Dictionary<FOMarkov, Dictionary<wD, double>> _WeightCache = new Dictionary<FOMarkov, Dictionary<wD, double>>();
        static Dictionary<SOMarkov, Dictionary<wD, double>> _Weight2Cache = new Dictionary<SOMarkov, Dictionary<wD, double>>();

        public static Dictionary<wD, double> _Funcs = new Dictionary<wD, double>()
            {
                { Weighting.FavourAlliteration, 0.5},
                { Weighting.FavourRarity, 0.5},
                { Weighting.FavourCommonness, 0.5},
                { Weighting.FavourAlternatingCommonness, 0.5},
                { Weighting.FavourDifferentLengths, 0.5},
                { Weighting.FavourSameLengths, 0.5},
                { Weighting.FavourNextPlural, 0.5},
                { Weighting.FavourVowels, 0.5},
                { Weighting.FavourConsonants, 0.5},
                { Weighting.FavourTotalSameness, 0.5},
                { Weighting.FavourTotalUniqueness, 0.5}
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

            foreach (var func in _Funcs.Keys.ToList())
            {
                _Funcs[func] = 0;
            }

            _Funcs[Weighting.FavourAlliteration] = 100;
            LearnFirstWeights(words, firsts, 0.4);
            int divisions = words.Count / 50;

            for (int i = 0; i < 50; i++)
            {
                var headlineToTest = words[i * divisions + rand.Next(divisions)];
                Console.WriteLine(GenerateFOSentence(headlineToTest[0], firsts, headlineToTest.Count));
                //var compWords = (double)headlineToTest.Zip(generated.Split(' ')).Sum(x => x.First == x.Second ? 1 : 0) / headlineToTest.Count;
            }

            int mostAccurateFOConfig = 0;
            double mostAccurateFOValue = 0;
            //833, 0.193
            int mostAccurateSOConfig = 0;
            double mostAccurateSOValue = 0;
            //836, 0.498
            for (int change = 0; change < 100000; change++)
            {
                _Funcs[Weighting.FavourAlternatingCommonness] = (double)change % 10;
                _Funcs[Weighting.FavourTotalUniqueness] = (double)(change / 10) % 10;
                _Funcs[Weighting.FavourDifferentLengths] = (double)(change / 100) % 10;
                _Funcs[Weighting.FavourNextPlural] = (double)(change / 1000) % 10;
                _Funcs[Weighting.FavourVowels] = (double)(change / 10000) % 10;

                LearnFirstWeights(words, firsts, 0.4);

                double avgFOaccuracy = 0;
                for (int i = 0; i < 20; i++)
                {
                    var headlineToTest = words[i * divisions + rand.Next(divisions)];
                    var generated = GenerateFOSentence(headlineToTest[0], firsts, headlineToTest.Count);
                    var compWords = (double)headlineToTest.Zip(generated.Split(' ')).Sum(x => x.First == x.Second ? 1 : 0) / headlineToTest.Count;
                    avgFOaccuracy += compWords / 20;
                }

                /* double avgSOaccuracy = 0;
                for (int i = 0; i < 50; i++)
                {
                    var headlineToTest = words[i * divisions + rand.Next(divisions)];
                    var generated = GenerateSOSentence(headlineToTest[0], seconds, headlineToTest.Count);
                    var compWords = (double)headlineToTest.Zip(generated.Split(' ')).Sum(x => x.First == x.Second ? 1 : 0) / headlineToTest.Count;
                    avgSOaccuracy += compWords / 50;
                } */

                Console.WriteLine($"Config = {change}");
                Console.WriteLine($"Average FO accuracy = {avgFOaccuracy}");
                //Console.WriteLine($"Average SO accuracy = {avgSOaccuracy}");

                if (avgFOaccuracy > mostAccurateFOValue)
                {
                    mostAccurateFOValue = avgFOaccuracy;
                    mostAccurateFOConfig = change;
                }

                /* if (avgSOaccuracy > mostAccurateSOValue)
                {
                    mostAccurateSOValue = avgSOaccuracy;
                    mostAccurateSOConfig = change;
                } */
            }

        }

        public static void LearnFirstWeights(List<List<string>> headlines, List<FOMarkov> firsts, double bias)
        {
            foreach (var pair in firsts)
            {
                if (!_WeightCache.ContainsKey(pair))
                {
                    var temp = new Dictionary<wD, double>();
                    foreach (var func in _Funcs)
                    {
                        temp[func.Key] = func.Key(pair.Next, pair.Base);
                    }
                    _WeightCache[pair] = temp;
                }
                pair.Weight = bias + _WeightCache[pair].Sum(x => x.Value * _Funcs[x.Key]);
            }

            foreach (var g in firsts.GroupBy(x => x.Base))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }
        }

        public static void LearnSecondWeights(List<List<string>> headlines, List<SOMarkov> seconds, double bias)
        {
            foreach (var trip in seconds)
            {
                trip.Weight = bias;
                foreach (var func in _Funcs)
                    trip.Weight += func.Key(trip.Next, trip.Base, trip.Base2) * func.Value;
            }

            foreach (var g in seconds.GroupBy(x => new { x.Base, x.Base2 }))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }
        }

        public static void LearnWeights(List<List<string>> headlines, List<FOMarkov> firsts, List<SOMarkov> seconds, double bias)
        {
            LearnFirstWeights(headlines, firsts, bias);
            LearnSecondWeights(headlines, seconds, bias);
        }

        public static string GenerateFOSentence(string firstWord, List<FOMarkov> firsts, int maxLength)
        {
            Random rand = new Random();

            string currentword = firstWord;
            string sentence = currentword;
            for (int i = 1; i < maxLength; i++)
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
                sentence += " " + currentword;
            }
            return sentence;
        }

        public static string GenerateSOSentence(string firstWord, List<SOMarkov> seconds, int maxLength)
        {
            Random rand = new Random();

            var potSec = seconds.Where(x => x.Base == firstWord).ToList();
            string currentword = firstWord;
            string sentence = currentword;
            string secondWord = potSec[rand.Next(potSec.Count)].Base2;
            sentence += " " + secondWord;

            for (int i = 2; i < maxLength; i++)
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
                sentence += " " + secondWord;
            }
            return sentence;
        }

        public static void PrintTrainedSentences(string firstWord, List<FOMarkov> firsts, int maxLength) =>
            Console.WriteLine(GenerateFOSentence(firstWord, firsts, maxLength));

    }

}
