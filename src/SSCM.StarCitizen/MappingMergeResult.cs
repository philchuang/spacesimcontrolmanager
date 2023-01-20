using SSCM.Core;
using System.Text;

namespace SSCM.StarCitizen;

public class MappingMergeResult : MappingMergeResultBase<MappingData>
{
    public ComparisonResult<InputDevice> InputDiffs { get; init; }
    public ComparisonResult<Mapping> MappingDiffs { get; init; }

    public MappingMergeResult(MappingData current, MappingData updated, ComparisonResult<InputDevice> inputs, ComparisonResult<Mapping> mappings) : base(current, updated)
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