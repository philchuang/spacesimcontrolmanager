using SSCM.Core;
using System.Text;

namespace SSCM.Elite;

public class MappingMergeResult : MappingMergeResultBase<EDMappingData>
{
    public ComparisonResult<EDMapping> MappingDiffs { get; init; }
    public ComparisonResult<EDMappingSetting> SettingDiffs { get; init; }

    public MappingMergeResult(EDMappingData current, EDMappingData updated, ComparisonResult<EDMapping> mappings, ComparisonResult<EDMappingSetting> settings) : base(current, updated)
    {
        this.MappingDiffs = mappings;
        this.SettingDiffs = settings;
    }


    public override string ToString()
    {
        var sb = new StringBuilder();
        if (this.MappingDiffs.Any())
        {
            sb.Append(base.PrintDiffs(this.MappingDiffs, "mappings", PrintMapping));
        }
        if (this.SettingDiffs.Any())
        {
            sb.Append(base.PrintDiffs(this.SettingDiffs, "settings", PrintSetting));
        }
        return sb.ToString();
    }

    private static string PrintBinding(EDBinding binding, string type) => $"{type}{(binding.Preserve ? "*" : "")} = {binding.ToString()}";

    private static string PrintBindings(EDMapping mapping)
    {
        var sb = new StringBuilder();
        if (mapping.Binding != null)
        {
            sb.AppendLine(PrintBinding(mapping.Binding, nameof(mapping.Binding)));
        }
        if (mapping.Primary != null)
        {
            sb.AppendLine(PrintBinding(mapping.Primary, nameof(mapping.Primary)));
        }
        if (mapping.Secondary != null)
        {
            sb.AppendLine(PrintBinding(mapping.Secondary, nameof(mapping.Secondary)));
        }
        return sb.ToString();
    }

    private static string PrintMapping(EDMapping mapping) => $"{mapping.Id}:\n{PrintBindings(mapping)}\n{mapping.Settings.Select(PrintSetting)}";

    private static string PrintSetting(EDMappingSetting setting) => $"{setting.Name} = {setting.Value}";
}