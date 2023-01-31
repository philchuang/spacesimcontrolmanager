namespace SSCM.Core;

public interface IMappingExporter<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string Backup();
    string RestoreLatest();
    Task<bool> Preview(TData source);
    Task<bool> Update(TData source);
}
