using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporterMarkdown
{
    public MappingReporterMarkdown()
    {
    }

    public string Report(EDMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Markdown => ReportMarkdown(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }

    private static string ReportMarkdown(EDMappingData data, ReportingOptions options)
    {
        var reportingMap = CreateReportingMap(data, options);

        var sb = new StringBuilder();

        sb.AppendLine("# Captured Elite: Dangerous mappings");
        sb.AppendLine();
        if (options.PreservedOnly)
        {
            sb.AppendLine("Only mappings marked to preserve.");
            sb.AppendLine();
        }

        var outputBinding = (string mappingName, string bindingOrdinal, EDBinding? binding) => {
            if (binding != null && (!options.PreservedOnly || binding.Preserve))
                sb.AppendLine($"{mappingName}.{bindingOrdinal}{(binding.Preserve ? "&ast;" : "")} = {(binding.Key.Device != "{NoDevice}" ? $"`{binding}`" : "[UNBOUND]")}  ");
        };

        foreach (var group in reportingMap)
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            
            foreach (var item in group.Value)
            {
                if (item.Value is EDMapping m)
                {
                    if (options.HeadersOnly)
                    {
                        sb.AppendLine($"{item.Key}{(m.AnyPreserve ? "&ast;" : "")}  ");
                    }
                    else
                    {
                        outputBinding(item.Key, nameof(m.Binding), m.Binding);
                        outputBinding(item.Key, nameof(m.Primary), m.Primary);
                        outputBinding(item.Key, nameof(m.Secondary), m.Secondary);
                    }
                }
                else if (item.Value is EDMappingSetting s)
                {
                    if (!options.PreservedOnly || s.Preserve)
                    {
                        sb.Append($"{item.Key}{(s.Preserve ? "&ast;" : "")}");
                        if (!options.HeadersOnly)
                            sb.Append($" = `{s.Value}`");
                        sb.AppendLine("  ");
                    }
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static SortedDictionary<string, SortedDictionary<string, object>> CreateReportingMap(EDMappingData data, ReportingOptions options)
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
}