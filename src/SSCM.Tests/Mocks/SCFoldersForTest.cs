using SSCM.Core.StarCitizen;

namespace SSCM.Tests.Mocks;

public class SCFoldersForTest : ISCFolders
{
    public string ActionMapsDir { get; private set; }
    public string SscmDataDir { get; private set; }

    public SCFoldersForTest(string? actionmapsDir = null, string? sscmDataDir = null)
    {
        this.ActionMapsDir = actionmapsDir ?? string.Empty;
        this.SscmDataDir = sscmDataDir ?? string.Empty;
    }
}