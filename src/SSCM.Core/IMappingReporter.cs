namespace SSCM.Core;

public enum ReportingFormat
{
    None,
    Csv,
    Markdown,
    Json
}

public interface IMappingReporter<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string Report(TData data, bool preservedOnly, ReportingFormat format);
}
