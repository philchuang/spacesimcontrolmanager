using NUnit.Framework;

namespace SCCM.Tests;

public static class Assert2
{
    public static void ListLength<T>(int length, IList<T> list)
    {
        Assert.NotNull(list);
        Assert.AreEqual(length, list.Count);
    }

    public static void ListEquals<T>(IList<T> expected, IList<T> actual, Action<T, T>? equate = null)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.AreEqual(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            if (equate != null)
            {
                equate(expected[i], actual[i]);
                continue;
            }

            Assert.AreEqual(expected[i], actual[i]);
        }
        
    }

    public static void DictionaryEquals<T,V>(IDictionary<T, V> expected, IDictionary<T, V> actual, Action<V, V>? equate = null)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.AreEqual(expected.Count, actual.Count);
        foreach (var kvp in expected)
        {
            Assert.True(actual.TryGetValue(kvp.Key, out var aValue));
            if (equate != null)
            {
                equate(kvp.Value, aValue);
                continue;
            }

            Assert.AreEqual(kvp.Value, aValue);
        }
        
    }
}