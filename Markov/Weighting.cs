using System;
using System.Collections.Generic;
using System.Linq;

public static class Weighting
{
    public static Dictionary<string, int> WordInstances = new Dictionary<string, int>();
    public static Dictionary<string, double> RelativeOccurence = new Dictionary<string, double>();

    public static double FavourDifferentLengths(string next, params string[] words)
    {
        var avgLen = (double)words.Sum(x => x.Length) / words.Length;
        //if avgLen = next.Length, p = 1, else p approaches 0
        return Math.Abs(avgLen - next.Length) / (avgLen + next.Length);  //0 < x < 1
    }

    public static double FavourSameLengths(string next, params string[] words) =>
        1 - FavourDifferentLengths(next, words);

    public static double FavourTotalUniqueness(string next, params string[] words)
    {
        string newStr = string.Concat(words, next);
        return (double)newStr.Distinct().Count() / newStr.Length;
    }

    public static double FavourTotalSameness(string next, params string[] words) =>
        1 - FavourTotalUniqueness(next, words);

    public static double FavourAlliteration(string next, params string[] words)
    {
        double ret = 0;
        ret += (next[0] == words.Last()[0]) ? 0.7 : 0;
        if (words.Last().Length > 2 && next.Length > 2)
        {
            ret += (next[1] == words.Last()[1]) ? 0.2 : 0;
            ret += (next[2] == words.Last()[2]) ? 0.1 : 0;
        }
        return ret;
    }

    public static double WeightingSample(string next, params string[] words)
    {
        return FavourTotalUniqueness(next, words) * 0.6 + FavourDifferentLengths(next, words) * 0.3 + FavourAlliteration(next, words) * 0.1;
    }

    public static double FavourNextPlural(string next, params string[] words) =>
        next.Last() == 's' ? 1 : 0;

    public static double FavourVowels(string next, params string[] words)
    {
        var totString = string.Concat(next, words);
        return (double)totString.Count(x => "aeiouy".Contains(x)) / totString.Length;
    }

    public static double FavourConsonants(string next, params string[] words) =>
        1 - FavourVowels(next, words);

    public static double FavourCommonness(string next, params string[] words) =>
        (RelativeOccurence[next] + words.Sum(x => RelativeOccurence[x])) / (words.Length + 1);

    public static double FavourRarity(string next, params string[] words) =>
        1 - FavourCommonness(next, words);
    
    public static double FavourAlternatingCommonness(string next, params string[] words)
    {
        var nextCommon = FavourCommonness(next, next);
        var lastCommon = FavourCommonness(words.Last(), words.Last());
        return Math.Abs(nextCommon - lastCommon);
    }

}
