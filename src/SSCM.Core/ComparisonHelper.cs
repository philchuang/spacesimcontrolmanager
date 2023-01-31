namespace SSCM.Core;

public static class ComparisonHelper
{
    public static ComparisonResult<T> Compare<T>(
        IList<T> current, 
        IList<T> updated, 
        Func<T, string> keyGenerator, 
        Func<T, T, bool> comparer)
    {
        Dictionary<string, T> currentMap = current.ToDictionary(keyGenerator);
        Dictionary<string, T> updatedMap = updated.ToDictionary(keyGenerator);

        return Compare(currentMap, updatedMap, comparer);
    }

    public static ComparisonResult<T> Compare<T>(
        IDictionary<string, T> current, 
        IDictionary<string, T> updated, 
        Func<T, T, bool> comparer)
    {
        var result = new ComparisonResult<T>();

        result.Removed.AddRange(current.Keys.Except(updated.Keys).OrderBy(s => s).Select(s => current[s]));
        result.Added.AddRange(updated.Keys.Except(current.Keys).OrderBy(s => s).Select(s => updated[s]));
        result.Changed.AddRange(updated
            .Where(ukvp => current.ContainsKey(ukvp.Key))
            .Select(ukvp => new ComparisonPair<T> { Key = ukvp.Key, Current = current[ukvp.Key], Updated = ukvp.Value })
            .Where(pair => !comparer(pair.Current, pair.Updated)));
        
        return result;
    }

    public static bool DictionariesAreEqual<K, V>(IDictionary<K, V> left, IDictionary<K, V> right, Func<V, V, bool>? comparer = null)
    {
        if (object.ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        if (left.Count != right.Count) return false;
        if (left.Keys.Except(right.Keys).Any()) return false;

        if (comparer == null) comparer = (v1, v2) => object.Equals(v1, v2);

        return left.All(lkvp => {
            if (!right.TryGetValue(lkvp.Key, out var rval)) return false;
            if (lkvp.Value == null && rval == null) return true;
            if (lkvp.Value == null || rval == null) return false;
            return comparer(lkvp.Value, rval);
        });
    }
}

#pragma warning disable CS8618

public class ComparisonPair<T>
{
    public string Key { get; init; }
    public T Current { get; init; }
    public T Updated { get; init; }
}

#pragma warning restore CS8618

public class ComparisonResult<T>
{
    public List<T> Added { get; set; } = new List<T>();
    public List<T> Removed { get; set; } = new List<T>();
    public List<ComparisonPair<T>> Changed { get; set; } = new List<ComparisonPair<T>>();

    public bool Any()
    {
        return this.Added.Any() || this.Removed.Any() || this.Changed.Any();
    }
}