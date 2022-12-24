using System.Text;

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

        // TODO if product ID has changed, report must overwrite
        if (inputDiffs.Any()) this.PrintDiffs(inputDiffs, "inputs", PrintInputDevice);
        if (mappingDiffs.Any()) this.PrintDiffs(mappingDiffs, "mappings", PrintMapping);
        return inputDiffs.Any() || mappingDiffs.Any();
    }

    private void PrintDiffs<T>(ComparisonResult<T> comp, string type, Func<T, string> formatter)
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
                this.StandardOutput($"CURRENT [{changed.Key}] = {formatter(changed.Current)}");
                this.StandardOutput($"UPDATED [{changed.Key}] = {formatter(changed.Updated)}");
            }
        }
        this.StandardOutput("");
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

    public MappingData Merge(MappingData current, MappingData updated)
    {
        /* what to do:
         * 1. input ID changed - can't merge because that would change all the bindings
         * 2. input setting added - add with preserve = true
         * 3. input setting removed - remove if preserve == false
         * 4. input setting changed - ?
         * 5. binding added - ?
         * 6. binding removed - ?
         * 7. binding changed - ?
         */
        return current;
    }
}