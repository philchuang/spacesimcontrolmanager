using System.Text;
using Newtonsoft.Json;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingReporterJson : EDMappingReporterBase
{
    protected override ReportingFormat Format => ReportingFormat.Json;

    public MappingReporterJson()
    {
    }

    protected override string ReportFull(EDMappingData data, ReportingOptions options)
    {
        var copy = data.JsonCopy();
        ReportJson(copy, options);
        return Serialize(copy);
    }

    private string Serialize(EDMappingData data) => JsonConvert.SerializeObject(data);

    private void ReportJson(EDMappingData data, ReportingOptions options)
    {
        ReportMappingsJson(data, options);
        ReportSettingsJson(data, options);
    }

    private void ReportMappingsJson(EDMappingData data, ReportingOptions options)
    {
        var clearBindingValues = (EDBinding b) => {
            b.Key.Device = string.Empty;
            b.Key.Key = string.Empty;
            b.Modifiers.Clear();
        };

        var mappings = data.Mappings.OrderBy(m => m.Group).ThenBy(m => m.Name).ToList();
        data.Mappings.Clear();
        foreach (var mapping in mappings)
        {
            if (options.PreservedOnly)
            {
                if (!mapping.AnyPreserve) continue;

                if (mapping.Binding?.Preserve != true) mapping.Binding = null;
                if (mapping.Primary?.Preserve != true) mapping.Primary = null;
                if (mapping.Secondary?.Preserve != true) mapping.Secondary = null;
                mapping.Settings = mapping.Settings.Where(s => s.Preserve).ToList();
            }

            if (options.HeadersOnly)
            {
                if (mapping.Binding != null) clearBindingValues(mapping.Binding);
                if (mapping.Primary != null) clearBindingValues(mapping.Primary);
                if (mapping.Secondary != null) clearBindingValues(mapping.Secondary);

            }

            data.Mappings.Add(mapping);
        }
    }

    private void ReportSettingsJson(EDMappingData data, ReportingOptions options)
    {
        var settings = data.Settings.OrderBy(s => s.Group).ThenBy(s => s.Name).ToList();
        data.Settings.Clear();
        foreach (var setting in settings)
        {
            if (options.PreservedOnly && !setting.Preserve) continue;

            if (options.HeadersOnly)
            {
                setting.Value = string.Empty;
            }

            data.Settings.Add(setting);
        }
    }
}