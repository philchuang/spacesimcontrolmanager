using SCCM.Core;

namespace SCCM.Tests.Mocks;

public class SCFoldersForTest : ISCFolders
{
    public string ActionMapsDir { get; private set; }
    public string SccmDir { get; private set; }

    public SCFoldersForTest(string? actionmapsDir = null, string? sccmDir = null)
    {
        this.ActionMapsDir = actionmapsDir ?? string.Empty;
        this.SccmDir = sccmDir ?? string.Empty;
    }
}