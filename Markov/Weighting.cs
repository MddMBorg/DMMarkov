using System;
using System.Linq;

public static class Weighting
{
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

}
