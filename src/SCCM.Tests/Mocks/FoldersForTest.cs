using SCCM.Core;

namespace SCCM.Tests.Mocks;

public class FoldersForTest : IFolders
{
    public string ActionMapsDir { get; private set; }
    public string SccmDir { get; private set; }

    public FoldersForTest(string? actionmapsDir = null, string? sccmDir = null)
    {
        this.ActionMapsDir = actionmapsDir ?? string.Empty;
        this.SccmDir = sccmDir ?? string.Empty;
    }
}