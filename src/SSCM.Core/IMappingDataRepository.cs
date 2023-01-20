namespace SSCM.Core;

public interface IMappingDataRepository<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string MappingDataSavePath { get; set; }

    TData CreateNew();

    Task<TData?> Load(string? saveFilePath = null);

    Task Save(TData data, string? saveFilePath = null);

    string? Backup();

    string? RestoreLatest();
}