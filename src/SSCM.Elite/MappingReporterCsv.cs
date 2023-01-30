using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporterCsv
{
    private const string MAPPING_HEADER = @"Group,Name,Preserve,Type,Binding";
    private const string SETTING_HEADER = @"Group,Name,Preserve,Value";

    public MappingReporterCsv()
    {
    }

    public string Report(EDMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => ReportCsv(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }

    private static string ReportCsv(EDMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();

        ReportMappingsCsv(data, options, sb);
        ReportSettingsCsv(data, options, sb);

        return sb.ToString();
    }

    private static void ReportMappingsCsv(EDMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Mappings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(MAPPING_HEADER);

        var helper = (EDMapping m, EDBinding? b, string type, bool preservedOnly) => {
            if (b != null && (!preservedOnly || b.Preserve))
            {
                WriteBinding(m.Group, m.Name, type, b, sb);
            }
        };

        foreach (var m in data.Mappings
            .Where(m => 
                !options.PreservedOnly || 
                m.Binding?.Preserve == true || 
                m.Primary?.Preserve == true || 
                m.Secondary?.Preserve == true))
        {
            if (options.HeadersOnly)
            {
                sb.AppendLine($"{m.Group},{m.Name},{m.AnyPreserve},,");
            }
            else
            {
                helper(m, m.Binding, nameof(m.Binding), options.PreservedOnly);
                helper(m, m.Primary, nameof(m.Primary), options.PreservedOnly);
                helper(m, m.Secondary, nameof(m.Secondary), options.PreservedOnly);
            }
        }
    }

    private static void WriteBinding(string group, string name, string type, EDBinding binding, StringBuilder sb)
    {
        sb.AppendLine($"{group},{name},{binding.Preserve},{type},{binding}");
    }

    private static void ReportSettingsCsv(EDMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Settings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(SETTING_HEADER);
        foreach (var s in data.Settings.Concat(data.Mappings.SelectMany(m => m.Settings))
            .Where(s => 
                !options.PreservedOnly || 
                s.Preserve))
        {
            if (options.HeadersOnly)
            {
                sb.AppendLine($"{s.Group},{s.Name},{s.Preserve},");
            }
            else
            {
                sb.AppendLine($"{s.Group},{s.Name},{s.Preserve},{s.Value}");
            }
        }
    }
}