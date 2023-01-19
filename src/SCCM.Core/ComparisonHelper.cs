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

        result.RemovedKeys.AddRange(currentMap.Keys.Except(updatedMap.Keys).OrderBy(s => s));
        result.AddedKeys.AddRange(updatedMap.Keys.Except(currentMap.Keys).OrderBy(s => s));
        result.ChangedPairs.AddRange(updatedMap
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

        return left.Where(lkvp => {
            if (!right.TryGetValue(lkvp.Key, out var rval)) return false;
            if (lkvp.Value == null && rval == null) return false;
            if (lkvp.Value == null || rval == null) return true;
            return lkvp.Value.Equals(rval);
        })
        .Any();
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
    public List<string> AddedKeys { get; set; } = new List<string>();
    public List<string> RemovedKeys { get; set; } = new List<string>();
    public List<ComparisonPair<T>> ChangedPairs { get; set; } = new List<ComparisonPair<T>>();

    public bool Any()
    {
        return this.AddedKeys.Any() || this.RemovedKeys.Any() || this.ChangedPairs.Any();
    }
}