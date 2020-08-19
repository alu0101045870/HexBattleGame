using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils 
{
    public static void FisherYatesShuffle<T>(this IList<T> list)
    { 
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Swap<T>(list[k], list[n]);
        }
    }

    public static void Swap<T>(T a, T b)
    {
        T value = a;
        a = b;
        b = value;
    }
}
