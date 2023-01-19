using System.Text;

namespace SCCM.Core;

public class MappingMergeResult
{
    public ComparisonResult<InputDevice> InputDiffs { get; init; }
    public ComparisonResult<Mapping> MappingDiffs { get; init; }

    public bool HasDifferences { get; set; }
    public bool CanMerge { get; set; }
    public IList<MappingMergeAction> MergeActions { get; set; } = new List<MappingMergeAction>();

    public MappingMergeResult(ComparisonResult<InputDevice> inputs, ComparisonResult<Mapping> mappings)
    {
        this.InputDiffs = inputs;
        this.MappingDiffs = mappings;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (this.InputDiffs.Any())
        {
            if (this.InputDiffs.HasChangedInputInstanceId())
            {
                sb.Append("WARNING: Input Instance IDs have changed and prevents a merge. Please manually resolve or execute import overwrite.");
            }
            sb.Append(this.PrintDiffs(this.InputDiffs, "inputs", PrintInputDevice));
        }
        if (this.MappingDiffs.Any())
        {
            sb.Append(this.PrintDiffs(this.MappingDiffs, "mappings", PrintMapping));
        }
        return sb.ToString();
    }

    private string PrintDiffs<T>(ComparisonResult<T> comp, string type, Func<T, string> formatter)
    {
        var sb = new StringBuilder();
        if (comp.AddedKeys.Any())
        {
            sb.AppendLine($"The following {type} were added: [{string.Join(", ", comp.AddedKeys)}]");
        }
        if (comp.RemovedKeys.Any())
        {
            sb.AppendLine($"The following {type} were removed: [{string.Join(", ", comp.RemovedKeys)}]");
        }
        if (comp.ChangedPairs.Any())
        {
            sb.AppendLine($"The following {type} were modified:");
            foreach (var changed in comp.ChangedPairs)
            {
                sb.AppendLine($"CURRENT [{changed.Key}] = {formatter(changed.Current)}");
                sb.AppendLine($"UPDATED [{changed.Key}] = {formatter(changed.Updated)}");
            }
        }
        sb.AppendLine("");
        return sb.ToString();
    }

    private static string PrintDictionary(IDictionary<string, string> dict)
    {
        return string.Join(", ", dict.Select(kvp => $"{kvp.Key} = {kvp.Value}"));
    }

    private static string PrintInputDevice(InputDevice input)
    {
        var sb = new StringBuilder();
        sb.Append($"{input.Type}-{input.Instance}, Settings = [");
        foreach (var setting in input.Settings)
        {
            sb.Append($"{setting.Name}: [{PrintDictionary(setting.Properties)}]\n");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private static string PrintMapping(Mapping mapping)
    {
        return $"{mapping.Input}{(mapping.MultiTap != null ? $" multitap = {mapping.MultiTap}" : "")}";
    }
}