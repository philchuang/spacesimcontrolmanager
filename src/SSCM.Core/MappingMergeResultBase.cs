using System.Text;

namespace SSCM.Core;

public abstract class MappingMergeResultBase<TData>
{
    public TData Current { get; init; }
    public TData Updated { get; init; }

    public bool HasDifferences { get; set; }
    public bool CanMerge { get; set; }
    public IList<MappingMergeAction> MergeActions { get; set; } = new List<MappingMergeAction>();

    protected MappingMergeResultBase(TData current, TData updated)
    {
        this.Current = current;
        this.Updated = updated;
    }

    protected string PrintDiffs<T>(ComparisonResult<T> comp, string type, Func<T, string> formatter)
    {
        var sb = new StringBuilder();
        if (comp.Added.Any())
        {
            sb.AppendLine($"The following {type} were added: [{string.Join(", ", comp.Added)}]");
        }
        if (comp.Removed.Any())
        {
            sb.AppendLine($"The following {type} were removed: [{string.Join(", ", comp.Removed)}]");
        }
        if (comp.Changed.Any())
        {
            sb.AppendLine($"The following {type} were modified:");
            foreach (var changed in comp.Changed)
            {
                sb.AppendLine($"CURRENT [{changed.Key}] = {formatter(changed.Current)}");
                sb.AppendLine($"UPDATED [{changed.Key}] = {formatter(changed.Updated)}");
            }
        }
        sb.AppendLine("");
        return sb.ToString();
    }

    protected static string PrintDictionary(IDictionary<string, string> dict)
    {
        return string.Join(", ", dict.Select(kvp => $"{kvp.Key} = {kvp.Value}"));
    }
}