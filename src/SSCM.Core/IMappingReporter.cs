namespace SSCM.Core;

public interface IMappingReporter<TData>
{
    string Report(TData data, bool preservedOnly);
    string ReportInputs(TData data, bool preservedOnly);
    string ReportMappings(TData data, bool preservedOnly);
}
