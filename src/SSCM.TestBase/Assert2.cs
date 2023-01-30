using NUnit.Framework;

namespace SSCM.Tests;

public static class Assert2
{
    public static void ListLength<T>(int length, IList<T> list)
    {
        Assert.NotNull(list);
        Assert.AreEqual(length, list.Count);
    }

    public static void EnumerableEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual, Action<T, T>? equate = null)
    {
        if (ReferenceEquals(expected, actual)) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        equate = equate ?? ((e, a) => Assert.AreEqual(e, a));

        var iterE = expected.GetEnumerator();        
        var iterA = actual.GetEnumerator();        

        var i = 0;
        try
        {
            while (iterE.MoveNext())
            {
                Assert.True(iterA.MoveNext());
                equate(iterE.Current, iterA.Current);
                i++;
            }

            Assert.False(iterA.MoveNext());
        }
        catch (AssertionException ae)
        {
            throw new AssertionException($"Assertion failed at index {i}:\n{ae.Message}!", ae);
        }
    }

    public static void DictionaryEquals<K,V1,V2>(IDictionary<K, V1> expected, IDictionary<K, V2> actual, bool expectedOnly = false, Action<V1, V2>? assert = null)
    {
        if (ReferenceEquals(expected, actual)) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        assert = assert ?? ((e, a) => Assert.AreEqual(e, a));

        HashSet<K> actualKeys = new HashSet<K> (actual.Keys);
        K lastKey;
        foreach (var kvp in expected)
        {
            lastKey = kvp.Key;
            try
            {
                Assert.True(actual.TryGetValue(kvp.Key, out var aValue));

                // silly unreachable code to get rid of warnings
                if (aValue == null) continue;

                assert(kvp.Value, aValue);
                actualKeys.Remove(kvp.Key);
            }
            catch (AssertionException ae)
            {
                throw new AssertionException($"Assertion failed on key {lastKey}:\n{ae.Message}!", ae);
            }
        }

        if (!expectedOnly)
        {
            Assert.AreEqual(0, actualKeys.Count);
        }
    }
}