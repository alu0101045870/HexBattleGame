using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils 
{
    public static void FisherYatesShuffle<T>(List<T> list)
    { 
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Swap<T>(k, n, ref list);
        }
    }

    public static void Swap<T>(int k, int n, ref List<T> list)
    {
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
    }

    public static void Swap<T>(T a , T b)
    {
        T value = a;
        a = b;
        b = value;
    }
}

public class Pair<T, U>
{
    public Pair() { }

    public Pair(T first, U second)
    {
        this.First = first;
        this.Second = second;
    }

    public T First { get; set; }
    public U Second { get; set; }
}