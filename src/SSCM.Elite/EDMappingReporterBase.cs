using System.Text;
using SSCM.Core;

namespace SSCM.Elite;

public abstract class EDMappingReporterBase
{
    protected abstract ReportingFormat Format { get; }

    protected EDMappingReporterBase()
    {
    }

    public string Report(EDMappingData data, ReportingOptions options)
    {
        if (options.Format != this.Format)
            throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");

        return ReportFull(data, options);
    }

    protected abstract string ReportFull(EDMappingData data, ReportingOptions options);

    protected static SortedDictionary<string, SortedDictionary<string, object>> CreateReportingMap(EDMappingData data)
    {
        var allGroupNames = data.Mappings.Select(m => m.Group).Concat(data.Settings.Select(s => s.Group)).Distinct().ToList();
        var reportingMap = new SortedDictionary<string, SortedDictionary<string, object>>();
        foreach (var m in data.Mappings)
        {
            if (!reportingMap.TryGetValue(m.Group, out var map))
            {
                map = new SortedDictionary<string, object>();
                reportingMap[m.Group] = map;
            }

            map[m.Name] = m;

            foreach (var s in m.Settings)
            {
                map[$"{m.Name}.{s.Name}"] = s;
            }
        }
        foreach (var s in data.Settings)
        {
            if (!reportingMap.TryGetValue(s.Group, out var map))
            {
                map = new SortedDictionary<string, object>();
                reportingMap[s.Group] = map;
            }

            map[s.Name] = s;
        }

        return reportingMap;
    }
}