namespace SCCM.Core;

public class MappingMerger
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public bool Preview(MappingData current, MappingData updated)
    {
        // capture differences
        var inputDiffs = ComparisonHelper.Compare(
            current.Inputs, updated.Inputs,
            i => $"{i.Type}-{i.Product}",
            (p, c) => p.Instance != c.Instance ||
                ComparisonHelper.DictionariesAreDifferent(
                    p.Settings.ToDictionary(s => s.Name), 
                    c.Settings.ToDictionary(s => s.Name))
            );
        var mappingDiffs = ComparisonHelper.Compare(
            current.Mappings, updated.Mappings,
            m => $"{m.ActionMap}-{m.Action}",
            (p, c) => p.Input != c.Input || p.MultiTap != c.MultiTap);

        if (inputDiffs.Any()) this.PrintDiffs(inputDiffs, "inputs");
        if (mappingDiffs.Any()) this.PrintDiffs(mappingDiffs, "mappings");
        return inputDiffs.Any() && mappingDiffs.Any();
    }

    private void PrintDiffs<T>(ComparisonResult<T> comp, string type)
    {
        if (comp.AddedKeys.Any())
        {
            this.StandardOutput($"The following {type} were added: [{string.Join(", ", comp.AddedKeys)}]");
        }
        if (comp.RemovedKeys.Any())
        {
            this.StandardOutput($"The following {type} were removed: [{string.Join(", ", comp.RemovedKeys)}]");
        }
        if (comp.ChangedPairs.Any())
        {
            this.StandardOutput($"The following {type} were modified:");
            foreach (var changed in comp.ChangedPairs)
            {
                this.StandardOutput($"CURRENT [{changed.Key}] = {changed.Current}");
                this.StandardOutput($"UPDATED [{changed.Key}] = {changed.Updated}");
            }
        }
    }

    public MappingData Merge(MappingData current, MappingData updated)
    {
        // TODO implement
        return current;
    }
}