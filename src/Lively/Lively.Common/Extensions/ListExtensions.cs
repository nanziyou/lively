using System;
using System.Collections.Generic;

namespace Lively.Common.Extensions;

public static class ListExtensions
{
    private static readonly Random _sharedRng = new();

    /// <summary>
    /// Fisher-Yates shuffle in place.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list, Random rng = null)
    {
        rng ??= _sharedRng;
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
