namespace SSCM.Core;

public enum ReportingFormat
{
    None,
    Csv,
    Markdown,
    Json
}

public class ReportingOptions
{
    public ReportingFormat Format { get; set; } = ReportingFormat.Csv;

    public bool HeadersOnly { get; set; }
    
    public bool PreservedOnly { get; set; }
}

public interface IMappingReporter<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string Report(TData data, ReportingOptions options);
}
