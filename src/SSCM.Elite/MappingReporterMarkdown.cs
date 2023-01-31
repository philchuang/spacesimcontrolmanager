using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporterMarkdown : EDMappingReporterBase
{
    protected override ReportingFormat Format => ReportingFormat.Markdown;
    
    public MappingReporterMarkdown()
    {
    }

    protected override string ReportFull(EDMappingData data, ReportingOptions options)
    {
        var reportingMap = CreateReportingMap(data);

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
                    if (options.PreservedOnly && !m.AnyPreserve) continue;

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
                    if (options.PreservedOnly && !s.Preserve) continue;

                    sb.Append($"{item.Key}{(s.Preserve ? "&ast;" : "")}");
                    if (!options.HeadersOnly)
                        sb.Append($" = {(string.IsNullOrWhiteSpace(s.Value) ? "[EMPTY]" : $"`{s.Value}`")}");
                    sb.AppendLine("  ");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}