using System.Text;
using Newtonsoft.Json;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingReporterJson : SCMappingReporterBase
{
    protected override ReportingFormat Format => ReportingFormat.Json;

    public MappingReporterJson()
    {
    }

    public string ReportInputs(SCMappingData data, ReportingOptions options)
    {
        var copy = data.JsonCopy();
        switch (options.Format)
        {
            case ReportingFormat.Json: ReportInputsJson(copy, options); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        };
        return Serialize(copy);
    }

    public string ReportMappings(SCMappingData data, ReportingOptions options)
    {
        var copy = data.JsonCopy();
        switch (options.Format)
        {
            case ReportingFormat.Json: ReportMappingsJson(copy, options); break;
            default: throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");
        };
        return Serialize(copy);
    }

    private string Serialize(SCMappingData data) => JsonConvert.SerializeObject(data);

    protected override string ReportFull(SCMappingData data, ReportingOptions options)
    {
        var copy = data.JsonCopy();
        ReportInputsJson(copy, options);
        ReportMappingsJson(copy, options);
        return Serialize(copy);
    }

    private void ReportInputsJson(SCMappingData data, ReportingOptions options)
    {
        var inputs = data.Inputs.OrderBy(i => i.Type).ThenBy(i => i.Instance).ToList();
        data.Inputs.Clear();
        foreach (var input in inputs)
        {
            if (options.PreservedOnly && !input.Preserve && input.Settings.All(s => !s.Preserve)) continue;

            var settings = input.Settings.OrderBy(s => s.Name).ToList();
            input.Settings.Clear();
            foreach (var setting in settings)
            {
                if (options.PreservedOnly && !setting.Preserve) continue;

                setting.Properties = new SortedDictionary<string, string>(setting.Properties);

                if (options.HeadersOnly)
                {
                    foreach (var kvp in setting.Properties.ToList())
                    {
                        setting.Properties[kvp.Key] = string.Empty;
                    }
                }

                input.Settings.Add(setting);
            }
            
            data.Inputs.Add(input);
        }
    }

    private void ReportMappingsJson(SCMappingData data, ReportingOptions options)
    {
        var grouped = data.Mappings.GroupBy(m => m.ActionMap).OrderBy(g => g.Key).ToList();
        data.Mappings.Clear();
        foreach (var g in grouped)
        {
            foreach (var mapping in g.OrderBy(m => m.Action))
            {
                if (options.PreservedOnly && !mapping.Preserve) continue;

                if (options.HeadersOnly)
                {
                    mapping.InputType = string.Empty;
                    mapping.Input = string.Empty;
                    mapping.MultiTap = null;
                }
                data.Mappings.Add(mapping);
            }
        }
    }
}