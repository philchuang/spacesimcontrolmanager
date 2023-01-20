namespace SSCM.Core;

public interface IMappingReporter
{
    string Report(MappingData data, bool preservedOnly);
    string ReportInputs(MappingData data, bool preservedOnly);
    string ReportMappings(MappingData data, bool preservedOnly);
}
