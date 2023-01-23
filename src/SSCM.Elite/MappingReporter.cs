using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporter : IMappingReporter<EDMappingData>
{
    private const string MAPPING_HEADER = @"Group,Name,Preserve,Type,Binding,Settings";
    private const string SETTING_HEADER = @"Group,Name,Preserve,Value";

    public MappingReporter()
    {
    }

    public string Report(EDMappingData data, bool preservedOnly)
    {
        var sb = new StringBuilder();

        ReportMappings(data, preservedOnly, sb);
        ReportSettings(data, preservedOnly, sb);

        return sb.ToString();
    }

    private static void ReportMappings(EDMappingData data, bool preservedOnly, StringBuilder sb)
    {
        if (!data.Mappings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(MAPPING_HEADER);

        foreach (var m in data.Mappings
            .Where(m => 
                !preservedOnly || 
                m.Primary?.Preserve == true || 
                m.Secondary?.Preserve == true || 
                m.Settings.Any(s => s.Preserve)))
        {
            if (m.Primary != null && (!preservedOnly || m.Primary.Preserve))
            {
                WriteBinding(m.Group, m.Name, nameof(m.Primary), m.Primary, m.Settings, sb);
            }
        }
    }

    private static void WriteBinding(string group, string name, string type, EDBinding binding, IList<EDMappingSetting> settings, StringBuilder sb)
    {
        sb.AppendLine($"{group},{name},{binding.Preserve},{type},{binding},\"{string.Join(",", settings.Select(s => $"{s.Name}: {s.Value}"))}\"");
    }

    private static void ReportSettings(EDMappingData data, bool preservedOnly, StringBuilder sb)
    {
        if (!data.Settings.Any()) return;
        
        if (sb.Length > 0)
        {
            sb.AppendLine("\n");
        }

        sb.AppendLine(SETTING_HEADER);
        foreach (var s in data.Settings
            .Where(s => 
                !preservedOnly || 
                s.Preserve))
        {
            sb.AppendLine($"{s.Group},{s.Name},{s.Preserve},{s.Value}");
        }
    }
}