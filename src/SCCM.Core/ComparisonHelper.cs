namespace SCCM.Core;

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

        var result = new ComparisonResult<T>();

        result.Removed.AddRange(currentMap.Keys.Except(updatedMap.Keys).OrderBy(s => s).Select(s => currentMap[s]));
        result.Added.AddRange(updatedMap.Keys.Except(currentMap.Keys).OrderBy(s => s).Select(s => updatedMap[s]));
        result.Changed.AddRange(updatedMap
            .Where(ukvp => currentMap.ContainsKey(ukvp.Key))
            .Select(ukvp => new ComparisonPair<T> { Key = ukvp.Key, Current = currentMap[ukvp.Key], Updated = ukvp.Value })
            .Where(pair => comparer(pair.Current, pair.Updated)));
        
        return result;
    }

    public static bool DictionariesAreDifferent<K, V>(IDictionary<K,V> left, IDictionary<K,V> right)
    {
        if (left == null && right == null) return false;
        if (left == null || right == null) return true;
        if (left.Count != right.Count) return true;
        if (left.Keys.Except(right.Keys).Any()) return true;

        return left.Any(lkvp => {
            if (!right.TryGetValue(lkvp.Key, out var rval)) return true;
            if (lkvp.Value == null && rval == null) return false;
            if (lkvp.Value == null || rval == null) return true;
            return !lkvp.Value.Equals(rval);
        });
    }
}

public class ComparisonPair<T>
{
    public string Key { get; init; }
    public T Current { get; init; }
    public T Updated { get; init; }
}

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