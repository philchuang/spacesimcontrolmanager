using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporter : IMappingReporter<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private const string MAPPING_HEADER = @"Group,Name,Preserve,Type,Binding,Settings";
    private const string SETTING_HEADER = @"Group,Name,Preserve,Value";

    public MappingReporter()
    {
    }

    public string Report(EDMappingData data, bool preservedOnly, ReportingFormat format)
    {
        if (format == ReportingFormat.Csv) return ReportCsv(data, preservedOnly);
        else if (format == ReportingFormat.Markdown) return ReportMarkdown(data, preservedOnly);
        else throw new ArgumentOutOfRangeException($"Unable to report in format [{format.ToString()}]!");
    }

    private static SortedDictionary<string, SortedDictionary<string, object>> CreateReportingMap(EDMappingData data, bool preservedOnly)
    {
        var allGroupNames = data.Mappings.Select(m => m.Group).Concat(data.Settings.Select(s => s.Group)).Distinct().ToList();
        var reportingMap = new SortedDictionary<string, SortedDictionary<string, object>>();
        foreach (var m in data.Mappings)
        {
            if (!reportingMap.TryGetValue(m.Group, out var list))
            {
                list = new SortedDictionary<string, object>();
                reportingMap[m.Group] = list;
            }

            list[m.Name] = m;

            foreach (var s in m.Settings)
            {
                list[$"{m.Name}.{s.Name}"] = s;
            }
        }
        foreach (var s in data.Settings)
        {
            if (!reportingMap.TryGetValue(s.Group, out var list))
            {
                list = new SortedDictionary<string, object>();
                reportingMap[s.Group] = list;
            }

            list[s.Name] = s;
        }

        return reportingMap;
    }

    private static string ReportCsv(EDMappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();

        ReportMappings(data, preservedOnly, sb);
        ReportSettings(data, preservedOnly, sb);

        return sb.ToString();
    }

    private static void ReportMappings(EDMappingData data, bool preservedOnly, StringBuilder sb)
    {
        if (!data.Mappings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(MAPPING_HEADER);

        foreach (var m in data.Mappings
            .Where(m => 
                !preservedOnly || 
                m.Primary?.Preserve == true || 
                m.Secondary?.Preserve == true || 
                m.Settings.Any(s => s.Preserve)))
        {
            if (m.Primary != null && (!preservedOnly || m.Primary.Preserve))
            {
                WriteBinding(m.Group, m.Name, nameof(m.Primary), m.Primary, m.Settings, sb);
            }
            if (m.Secondary != null && (!preservedOnly || m.Secondary.Preserve))
            {
                WriteBinding(m.Group, m.Name, nameof(m.Secondary), m.Secondary, m.Settings, sb);
            }
        }
    }

    private static void WriteBinding(string group, string name, string type, EDBinding binding, IList<EDMappingSetting> settings, StringBuilder sb)
    {
        sb.AppendLine($"{group},{name},{binding.Preserve},{type},{binding},\"{string.Join(",", settings.Select(s => $"{s.Name}: {s.Value}"))}\"");
    }

    private static void ReportSettings(EDMappingData data, bool preservedOnly, StringBuilder sb)
    {
        if (!data.Settings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(SETTING_HEADER);
        foreach (var s in data.Settings
            .Where(s => 
                !preservedOnly || 
                s.Preserve))
        {
            sb.AppendLine($"{s.Group},{s.Name},{s.Preserve},{s.Value}");
        }
    }

    private static string ReportMarkdown(EDMappingData data, bool preservedOnly)
    {
        var reportingMap = CreateReportingMap(data, preservedOnly);

        var sb = new StringBuilder();

        sb.AppendLine("# Elite: Dangerous mappings from Custom binds file");
        sb.AppendLine();
        if (preservedOnly)
        {
            sb.AppendLine("Only mappings marked to preserve.");
            sb.AppendLine();
        }

        var outputBinding = (string mappingName, string bindingOrdinal, EDBinding? binding) => {
            if (binding != null && (!preservedOnly || binding.Preserve))
                sb.AppendLine($"{mappingName}.{bindingOrdinal}{(binding.Preserve ? "*" : "")} = {(binding.Key.Device != "{NoDevice}" ? binding.ToString() : "[UNBOUND]")}");
        };

        foreach (var group in reportingMap)
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            
            foreach (var item in group.Value)
            {
                if (item.Value is EDMapping m)
                {
                    outputBinding(item.Key, nameof(m.Primary), m.Primary);
                    outputBinding(item.Key, nameof(m.Secondary), m.Secondary);
                }
                else if (item.Value is EDMappingSetting s)
                {
                    if (!preservedOnly || s.Preserve)
                        sb.AppendLine($"{item.Key}{(s.Preserve ? "*" : "")} = {s.Value}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}