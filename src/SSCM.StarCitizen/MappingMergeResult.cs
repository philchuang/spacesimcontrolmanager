using SSCM.Core;
using System.Text;

namespace SSCM.StarCitizen;

public class MappingMergeResult : MappingMergeResultBase<SCMappingData>
{
    public ComparisonResult<SCInputDevice> InputDiffs { get; init; }
    public ComparisonResult<SCMapping> MappingDiffs { get; init; }
    public ComparisonResult<SCAttribute> AttributeDiffs { get; init; }

    public MappingMergeResult(SCMappingData current, SCMappingData updated, ComparisonResult<SCInputDevice> inputs, ComparisonResult<SCMapping> mappings, ComparisonResult<SCAttribute> attributes) : base(current, updated)
    {
        this.InputDiffs = inputs;
        this.MappingDiffs = mappings;
        this.AttributeDiffs = attributes;
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
        if (this.AttributeDiffs.Any())
        {
            sb.Append(this.PrintDiffs(this.AttributeDiffs, "attributes", PrintAttribute));
        }
        return sb.ToString();
    }

    private static string PrintInputDevice(SCInputDevice input)
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

    private static string PrintMapping(SCMapping mapping)
    {
        return mapping.InputToString;
    }

    private static string PrintAttribute(SCAttribute attribute)
    {
        return attribute.Value;
    }
}