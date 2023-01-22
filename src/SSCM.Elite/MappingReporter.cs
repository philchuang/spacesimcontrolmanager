using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporter : IMappingReporter<EDMappingData>
{
    private const string INPUT_HEADER = @"Id,Type,Name,Preserve,SettingNames";
    private const string MAPPING_HEADER = @"Group,Action,Preserve,InputType,Binding,Options";

    public MappingReporter()
    {
    }

    public string Report(EDMappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();

        throw new NotImplementedException();

        return sb.ToString();
    }

    public string ReportInputs(EDMappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();
        throw new NotImplementedException();
        return sb.ToString();
    }

    private static void ReportInputs(EDMappingData data, bool preservedOnly, StringBuilder sb)
    {
        throw new NotImplementedException();
    }

    public string ReportMappings(EDMappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();
        throw new NotImplementedException();
        return sb.ToString();
    }
}