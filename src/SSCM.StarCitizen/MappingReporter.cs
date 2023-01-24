using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingReporter : IMappingReporter<SCMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private const string INPUT_HEADER = @"Id,Type,Name,Preserve,SettingNames";
    private const string MAPPING_HEADER = @"Group,Action,Preserve,InputType,Binding,Options";

    public MappingReporter()
    {
    }

    public string Report(SCMappingData data, ReportingOptions options)
    {
        if (options.Format == ReportingFormat.Csv) return ReportCsv(data, options);
        else if (options.Format == ReportingFormat.Markdown) return ReportMarkdown(data, options);
        else throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
    }

    private string ReportCsv(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();

        ReportInputs(data, options, sb);
        ReportMappings(data, options, sb);

        return sb.ToString();
    }

    public string ReportInputs(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        ReportInputs(data, options, sb);
        return sb.ToString();
    }

    private static void ReportInputs(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Inputs.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(INPUT_HEADER);

        foreach (var input in data.Inputs.Where(i => !options.PreservedOnly || i.Preserve))
        {
            WriteInput(input, sb);
        }
    }

    private static void WriteInput(SCInputDevice input, StringBuilder sb)
    {
        sb.Append($"{input.Id},{input.Type},{input.Product},{input.Preserve},");
        if (input.Settings.Any()) sb.Append($"\"{string.Join(", ", input.Settings.Select(s => s.Name).OrderBy(s => s))}\"");
        sb.AppendLine();
    }

    public string ReportMappings(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        ReportMappings(data, options, sb);
        return sb.ToString();
    }

    private static void ReportMappings(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Mappings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(MAPPING_HEADER);

        foreach (var mapping in data.Mappings.Where(m => !options.PreservedOnly || m.Preserve))
        {
            WriteMapping(mapping, sb);
        }
    }

    private static void WriteMapping(SCMapping mapping, StringBuilder sb)
    {
        sb.Append($"{mapping.ActionMap},{mapping.Action},{mapping.Preserve},{mapping.InputType},{mapping.Input},");
        if (mapping.MultiTap != null) sb.Append($"\"MultiTap: {mapping.MultiTap}\"");
        sb.AppendLine();
    }

    private string ReportMarkdown(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Star Citizen mappings from actionmaps.xml file");
        sb.AppendLine();
        if (options.PreservedOnly)
        {
            sb.AppendLine("Only mappings marked to preserve.");
            sb.AppendLine();
        }

        sb.AppendLine("## Input Devices");
        sb.AppendLine();
        foreach (var input in data.Inputs)
        {
            if (options.PreservedOnly && !input.Preserve && input.Settings.All(s => !s.Preserve)) continue;

            sb.AppendLine($"### {input.Type}-{input.Instance}: [{input.Product}]{(input.Preserve ? "*" : "")}");
            foreach (var s in input.Settings)
                sb.AppendLine($"{s.Name}{(s.Preserve ? "*" : "")} = {string.Join(",", s.Properties.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
            sb.AppendLine();
        }

        sb.AppendLine("## Mapping Groups");
        sb.AppendLine();

        var grouped = data.Mappings.GroupBy(m => m.ActionMap).OrderBy(g => g.Key);

        foreach (var g in grouped)
        {
            sb.AppendLine($"## {g.Key}");
            sb.AppendLine();

            foreach (var mapping in g)
            {
                if (options.PreservedOnly && !mapping.Preserve) continue;
                
                sb.AppendLine($"{mapping.Action}{(mapping.Preserve ? "*" : "")} = {mapping.Input}{(mapping.MultiTap != null ? $" (multiTap {mapping.MultiTap.Value})" : "")}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}