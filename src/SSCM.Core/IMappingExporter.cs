namespace SSCM.Core;

public interface IMappingExporter<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    ExportOptions ExportOptions { get; set; }

    string Backup();
    string RestoreLatest();
    Task<bool> Preview(TData source);
    Task<bool> Update(TData source);
    Task<bool> UpdateInteractive(TData source, IUserInput userInput);
    Task<InteractiveChangeSession> CreateInteractiveSession(TData source);
    Task SaveInteractive();
}
