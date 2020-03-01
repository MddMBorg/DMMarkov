using System;
using System.Collections.Generic;
using System.Linq;

namespace Markov
{
    public class Train
    {
        delegate double wD(string next, params string[] words);

        public static void RandomTrain()
        {
            List<wD> funcs = new List<wD>()
            {
                Weighting.FavourAlliteration, Weighting.FavourRarity, Weighting.FavourCommonness,
                Weighting.FavourAlternatingCommonness, Weighting.FavourDifferentLengths, Weighting.FavourSameLengths,
                Weighting.FavourNextPlural, Weighting.FavourVowels, Weighting.FavourConsonants,
                Weighting.FavourTotalSameness, Weighting.FavourTotalUniqueness
            };

            Random rand = new Random();

            var funcBias = funcs.ToDictionary(x => x, x => rand.NextDouble());

            var words = ParseHelper.GetWordList();
            Weighting.WordInstances = words.SelectMany(x => x.Select(y => y)).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            double maxOccur = (double)Weighting.WordInstances.Max(x => x.Value);
            Weighting.RelativeOccurence = Weighting.WordInstances.ToDictionary(x => x.Key, x => x.Value / maxOccur);

            List<FOMarkov> firsts = new List<FOMarkov>();
            List<SOMarkov> seconds = new List<SOMarkov>();

            ParseHelper.GetLinks(words, firsts, seconds);

            foreach (var func in funcBias)
            {
                Console.WriteLine($"Function {func.Key.Method.Name} at bias {func.Value}");
            }

            int mostAccurateFOConfig = 0;
            double mostAccurateFOValue = 0;
//833, 0.193
            int mostAccurateSOConfig = 0;
            double mostAccurateSOValue = 0;
//836, 0.498
            for (int change = 0; change < 1000; change++)
            {
                funcBias[Weighting.FavourAlternatingCommonness] = (double)change % 10;
                funcBias[Weighting.FavourTotalUniqueness] = (double)(change / 10) % 10;
                funcBias[Weighting.FavourDifferentLengths] = (double)(change / 100) % 10;

                _LearnWeights(words, firsts, seconds, funcBias, 0.4);

                double avgFOaccuracy = 0;
                int divisions = words.Count / 50;
                for (int i = 0; i < 50; i++)
                {
                    var headlineToTest = words[i * divisions + rand.Next(divisions)];
                    var generated = GenerateFOSentence(headlineToTest[0], firsts, headlineToTest.Count);
                    var compWords = (double)headlineToTest.Zip(generated.Split(' ')).Sum(x => x.First == x.Second ? 1 : 0) / headlineToTest.Count;
                    avgFOaccuracy += compWords / 50;
                }

                double avgSOaccuracy = 0;
                for (int i = 0; i < 50; i++)
                {
                    var headlineToTest = words[i * divisions + rand.Next(divisions)];
                    var generated = GenerateSOSentence(headlineToTest[0], seconds, headlineToTest.Count);
                    var compWords = (double)headlineToTest.Zip(generated.Split(' ')).Sum(x => x.First == x.Second ? 1 : 0) / headlineToTest.Count;
                    avgSOaccuracy += compWords / 50;
                }

                Console.WriteLine($"Config = {change}");
                Console.WriteLine($"Average FO accuracy = {avgFOaccuracy}");
                Console.WriteLine($"Average SO accuracy = {avgSOaccuracy}");

                if (avgFOaccuracy > mostAccurateFOValue)
                {
                    mostAccurateFOValue = avgFOaccuracy;
                    mostAccurateFOConfig = change;
                }

                if (avgSOaccuracy > mostAccurateSOValue)
                {
                    mostAccurateSOValue = avgSOaccuracy;
                    mostAccurateSOConfig = change;
                }
            }

        }

        static void _LearnWeights(List<List<string>> headlines, List<FOMarkov> firsts, List<SOMarkov> seconds, Dictionary<wD, double> funcBias, double bias)
        {
            foreach (var pair in firsts)
            {
                pair.Weight = bias;
                foreach (var func in funcBias)
                    pair.Weight += func.Key(pair.Next, pair.Base) * func.Value;
            }

            foreach (var trip in seconds)
            {
                trip.Weight = bias;
                foreach (var func in funcBias)
                    trip.Weight += func.Key(trip.Next, trip.Base, trip.Base2) * func.Value;
            }

            foreach (var g in firsts.GroupBy(x => x.Base))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }

            foreach (var g in seconds.GroupBy(x => new { x.Base, x.Base2 }))
            {
                var totalP = g.Sum(x => x.Count * x.Weight);
                foreach (var i in g)
                    i.Probability = (double)i.Count * i.Weight / totalP;
            }
        }

        static string GenerateFOSentence(string firstWord, List<FOMarkov> firsts, int maxLength)
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

        static string GenerateSOSentence(string firstWord, List<SOMarkov> seconds, int maxLength)
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

    }

}