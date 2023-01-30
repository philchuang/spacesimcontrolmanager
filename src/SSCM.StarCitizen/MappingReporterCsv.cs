using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingReporterCsv
{
    private const string INPUT_HEADER = @"Id,Type,Name,Preserve,SettingNames";
    private const string MAPPING_HEADER = @"Group,Action,Preserve,InputType,Binding,Options";

    public MappingReporterCsv()
    {
    }

    public string Report(SCMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => ReportCsv(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }

    private string ReportCsv(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();

        ReportInputsCsv(data, options, sb);
        ReportMappingsCsv(data, options, sb);

        return sb.ToString();
    }

    public string ReportInputs(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        switch (options.Format)
        {
            case ReportingFormat.Csv: ReportInputsCsv(data, options, sb); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        }
        return sb.ToString();
    }

    private static void ReportInputsCsv(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Inputs.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(INPUT_HEADER);

        foreach (var input in data.Inputs.Where(i => !options.PreservedOnly || i.Preserve))
        {
            WriteInput(input, options, sb);
        }
    }

    private static void WriteInput(SCInputDevice input, ReportingOptions options, StringBuilder sb)
    {
        sb.Append($"{input.Id},{input.Type},{input.Product},{input.Preserve},");
        if (!options.HeadersOnly && input.Settings.Any()) sb.Append($"\"{string.Join(", ", input.Settings.Select(s => s.Name).OrderBy(s => s))}\"");
        sb.AppendLine();
    }

    public string ReportMappings(SCMappingData data, ReportingOptions options)
    {
        var sb = new StringBuilder();
        switch (options.Format)
        {
            case ReportingFormat.Csv: ReportMappingsCsv(data, options, sb); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        }
        return sb.ToString();
    }

    private static void ReportMappingsCsv(SCMappingData data, ReportingOptions options, StringBuilder sb)
    {
        if (!data.Mappings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(MAPPING_HEADER);

        foreach (var mapping in data.Mappings.Where(m => !options.PreservedOnly || m.Preserve))
        {
            WriteMapping(mapping, options, sb);
        }
    }

    private static void WriteMapping(SCMapping mapping, ReportingOptions options, StringBuilder sb)
    {
        sb.Append($"{mapping.ActionMap},{mapping.Action},{mapping.Preserve},");
        if (options.HeadersOnly)
        {
            sb.Append(",,");
        }
        else
        {
            sb.Append("${mapping.InputType},{mapping.Input},");
            if (mapping.MultiTap != null) sb.Append($"\"MultiTap: {mapping.MultiTap}\"");
        }
        sb.AppendLine();
    }
}