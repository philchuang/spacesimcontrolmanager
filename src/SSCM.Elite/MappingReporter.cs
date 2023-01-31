using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporter : IMappingReporter<EDMappingData>
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

    public string Report(EDMappingData data, ReportingOptions options)
    {
        return options.Format switch {
            ReportingFormat.Csv => this._csv.Report(data, options),
            ReportingFormat.Markdown => this._md.Report(data, options),
            ReportingFormat.Json => this._json.Report(data, options),
            _ => throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!"),
        };
    }
}