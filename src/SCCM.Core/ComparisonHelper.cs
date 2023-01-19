namespace SCCM.Core;

public static class ComparisonHelper
{
    public static ComparisonResult<T> Compare<T>(
        IList<T> previous, 
        IList<T> current, 
        Func<T, string> keyGenerator, 
        Func<T, T, bool> comparer)
    {
        Dictionary<string, T> currentMap = current.ToDictionary(keyGenerator);
        Dictionary<string, T> previousMap = previous.ToDictionary(keyGenerator);

        var result = new ComparisonResult<T>();

        result.RemovedKeys.AddRange(previousMap.Keys.Except(currentMap.Keys).OrderBy(s => s));
        result.AddedKeys.AddRange(currentMap.Keys.Except(previousMap.Keys).OrderBy(s => s));
        result.ChangedPairs.AddRange(currentMap
            .Where(ckvp => previousMap.ContainsKey(ckvp.Key))
            .Select(ckvp => new ComparisonPair<T> { Current = ckvp.Value, Previous = previousMap[ckvp.Key] })
            .Where(pair => comparer(pair.Previous, pair.Current)));
        
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
    public T Current { get; init; }
    public T Previous { get; init; }
}

public class ComparisonResult<T>
{
    public List<string> AddedKeys { get; set; } = new List<string>();
    public List<string> RemovedKeys { get; set; } = new List<string>();
    public List<ComparisonPair<T>> ChangedPairs { get; set; } = new List<ComparisonPair<T>>();
}