using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingReporterMarkdown : SCMappingReporterBase
{
    protected override ReportingFormat Format => ReportingFormat.Markdown;

    public MappingReporterMarkdown()
    {
    }

    public string ReportInputs(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        switch (options.Format)
        {
            case ReportingFormat.Markdown: ReportInputsMarkdown(data, options, sb); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        }
        return sb.ToString();
    }

    public string ReportMappings(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        switch (options.Format)
        {
            case ReportingFormat.Markdown: ReportMappingsMarkdown(data, options, sb); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        }
        return sb.ToString();
    }

    protected override string ReportFull(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Captured Star Citizen mappings");
        sb.AppendLine();
        if (options.PreservedOnly)
        {
            sb.AppendLine("Only mappings marked to preserve.");
            sb.AppendLine();
        }

        ReportInputsMarkdown(data, options, sb);
        ReportMappingsMarkdown(data, options, sb);

        return sb.ToString();
    }

    private void ReportInputsMarkdown(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        sb.AppendLine("## Input Devices");
        sb.AppendLine();
        foreach (var input in data.Inputs.OrderBy(i => i.Type).ThenBy(i => i.Instance))
        {
            if (options.PreservedOnly && !input.Preserve && input.Settings.All(s => !s.Preserve)) continue;

            sb.AppendLine($"### {input.Type}-{input.Instance}: [{input.Product}]{(input.Preserve ? "&ast;" : "")}\n");
            if (!options.HeadersOnly)
            {
                foreach (var s in input.Settings)
                {
                    sb.AppendLine($"{s.Name}{(s.Preserve ? "&ast;" : "")} = `{string.Join(",", s.Properties.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}`  \n");
                }
            }
        }
    }

    private void ReportMappingsMarkdown(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        sb.AppendLine("## Mapping Groups");
        sb.AppendLine();

        var grouped = data.Mappings.GroupBy(m => m.ActionMap).OrderBy(g => g.Key);

        foreach (var g in grouped)
        {
            sb.AppendLine($"## {g.Key}");
            sb.AppendLine();

            foreach (var mapping in g.OrderBy(m => m.Action))
            {
                if (options.PreservedOnly && !mapping.Preserve) continue;

                sb.Append($"{mapping.Action}{(mapping.Preserve ? "&ast;" : "")}");
                
                if (!options.HeadersOnly)
                {
                    sb.Append($" = `{mapping.Input}`{(mapping.MultiTap != null ? $" (multiTap {mapping.MultiTap.Value})" : "")}");
                }
                
                sb.AppendLine("  ");
            }

            sb.AppendLine();
        }
    }
}