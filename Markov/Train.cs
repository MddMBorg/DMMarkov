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

            var funcBias = funcs.Select(x => new { Func = x, Bias = rand.NextDouble() });

            var words = ParseHelper.GetWordList();
            var firstWords = words.Select(x => x.First()).ToList();
            Weighting.WordInstances = words.SelectMany(x => x.Select(y => y)).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            double maxOccur = (double)Weighting.WordInstances.Max(x => x.Value);
            Weighting.RelativeOccurence = Weighting.WordInstances.ToDictionary(x => x.Key, x => x.Value / maxOccur);

            List<FOMarkov> firsts = new List<FOMarkov>();
            List<SOMarkov> seconds = new List<SOMarkov>();

            ParseHelper.GetLinks(words, firsts, seconds);

            foreach (var pair in firsts)
            {
                pair.Weight = 0;
                foreach (var func in funcBias)
                    pair.Weight += func.Func(pair.Next, pair.Base) * func.Bias;
            }

            foreach (var trip in seconds)
            {
                trip.Weight = 0;
                foreach (var func in funcBias)
                    trip.Weight += func.Func(trip.Next, trip.Base, trip.Base2) * func.Bias;
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

            foreach (var func in funcBias)
            {
                Console.WriteLine($"Function {func.Func.Method.Name} at bias {func.Bias}");
            }

            Console.WriteLine("\nFirst order:\n");

            for (int k = 0; k < 50; k++)
            {
                string sentence = "";

                string currentword = firstWords[rand.Next(firstWords.Count)];
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
            }

            Console.WriteLine("\nSecond order:\n");

            for (int k = 0; k < 100; k++)
            {
                string sentence = "";
                var potSec = seconds.Where(x => x.Probability != 1 && firstWords.Contains(x.Base)).ToList();

                string currentword = potSec[rand.Next(potSec.Count)].Base;
                sentence = currentword;
                potSec = seconds.Where(x => x.Base == currentword && x.Probability != 1).ToList();
                string secondWord = potSec[rand.Next(potSec.Count)].Base2;
                sentence += " " + secondWord;

                for (int i = 0; i < 18; i++)
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

        }

    }

}
