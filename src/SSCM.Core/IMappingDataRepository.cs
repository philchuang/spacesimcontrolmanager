using SCCM.Core;

public interface IMappingDataRepository
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string MappingDataSavePath { get; set; }

    Task<MappingData?> Load(string? saveFilePath = null);

    Task Save(MappingData data, string? saveFilePath = null);

    string? Backup();

    string? RestoreLatest();
}