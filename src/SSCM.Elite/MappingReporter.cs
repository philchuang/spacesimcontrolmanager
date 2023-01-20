using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporter : IMappingReporter<MappingData>
{
    private const string INPUT_HEADER = @"Id,Type,Name,Preserve,SettingNames";
    private const string MAPPING_HEADER = @"Group,Action,Preserve,InputType,Binding,Options";

    public MappingReporter()
    {
    }

    public string Report(MappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();

        throw new NotImplementedException();

        return sb.ToString();
    }

    public string ReportInputs(MappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();
        throw new NotImplementedException();
        return sb.ToString();
    }

    private static void ReportInputs(MappingData data, bool preservedOnly, StringBuilder sb)
    {
        throw new NotImplementedException();
    }

    public string ReportMappings(MappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();
        throw new NotImplementedException();
        return sb.ToString();
    }
}