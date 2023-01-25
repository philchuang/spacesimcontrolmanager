using SSCM.Elite;

namespace SSCM.StarCitizen.Tests.Mocks;

public class EDFoldersForTest : IEDFolders
{
    public string GameConfigDir { get; private set; }
    public string GameConfigPath { get; private set; }
    public string EliteDataDir { get; private set; }

    public EDFoldersForTest(string? gameConfigDir = null, string? gameConfigPath = null, string? eliteDataDir = null)
    {
        this.GameConfigDir = gameConfigDir ?? string.Empty;
        this.GameConfigPath = gameConfigPath ?? string.Empty;
        this.EliteDataDir = eliteDataDir ?? string.Empty;
    }
}