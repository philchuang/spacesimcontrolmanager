namespace SSCM.Core;

public interface IMappingImporter<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    Task<TData> Read();
}
