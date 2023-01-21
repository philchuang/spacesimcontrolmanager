using SSCM.StarCitizen;

namespace SSCM.StarCitizen.Tests.Mocks;

public class SCFoldersForTest : ISCFolders
{
    public string GameConfigDir { get; private set; }
    public string ScDataDir { get; private set; }

    public SCFoldersForTest(string? gameConfigDir = null, string? scDataDir = null)
    {
        this.GameConfigDir = gameConfigDir ?? string.Empty;
        this.ScDataDir = scDataDir ?? string.Empty;
    }
}