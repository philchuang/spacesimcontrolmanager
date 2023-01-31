using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingReporter : IMappingReporter<SCMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private readonly MappingReporterCsv _csv;
    private readonly MappingReporterMarkdown _md;
    private readonly MappingReporterJson _json;

    public MappingReporter()
    {
        this._csv = new MappingReporterCsv();
        this._md = new MappingReporterMarkdown();
        this._json = new MappingReporterJson();
    }

    public string Report(SCMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => this._csv.Report(data, options),
            ReportingFormat.Markdown => this._md.Report(data, options),
            ReportingFormat.Json => this._json.Report(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }

    public string ReportInputs(SCMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => this._csv.ReportInputs(data, options),
            ReportingFormat.Markdown => this._md.ReportInputs(data, options),
            ReportingFormat.Json => this._json.ReportInputs(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }

    public string ReportMappings(SCMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => this._csv.ReportMappings(data, options),
            ReportingFormat.Markdown => this._md.ReportMappings(data, options),
            ReportingFormat.Json => this._json.ReportMappings(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }
}