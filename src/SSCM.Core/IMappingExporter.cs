namespace SSCM.Core;

public interface IMappingExporter
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string GameConfigPath { get; }

    string Backup();
    string RestoreLatest();
    Task<bool> Preview(MappingData source);
    Task<bool> Update(MappingData source);
}
