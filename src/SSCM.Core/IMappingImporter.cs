namespace SSCM.Core;

public interface IMappingImporter
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    Task<MappingData> Read();
}
