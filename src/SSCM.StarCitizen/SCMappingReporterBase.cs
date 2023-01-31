using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen;

public abstract class SCMappingReporterBase
{
    protected abstract ReportingFormat Format { get; }

    protected SCMappingReporterBase()
    {
    }

    public string Report(SCMappingData data, ReportingOptions options)
    {
        if (options.Format != this.Format)
            throw new ArgumentOutOfRangeException($"Unable to report in format [{options.Format.ToString()}]!");

        return ReportFull(data, options);
    }

    protected abstract string ReportFull(SCMappingData data, ReportingOptions options);
}