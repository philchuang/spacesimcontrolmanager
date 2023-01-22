using SSCM.Elite;

namespace SSCM.StarCitizen.Tests.Mocks;

public class EDFoldersForTest : IEDFolders
{
    public string GameConfigDir { get; private set; }
    public string GameConfigPath { get; private set; }
    public string EdDataDir { get; private set; }

    public EDFoldersForTest(string? gameConfigDir = null, string? gameConfigPath = null, string? edDataDir = null)
    {
        this.GameConfigDir = gameConfigDir ?? string.Empty;
        this.GameConfigPath = gameConfigPath ?? string.Empty;
        this.EdDataDir = edDataDir ?? string.Empty;
    }
}