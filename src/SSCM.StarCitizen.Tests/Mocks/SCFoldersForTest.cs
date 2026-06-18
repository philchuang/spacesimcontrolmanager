using SSCM.StarCitizen;

namespace SSCM.StarCitizen.Tests.Mocks;

public class SCFoldersForTest : ISCFolders
{
    public string Environment { get; set; } = "LIVE";
    public string GameAttributesPath { get; set; } = string.Empty;
    public string GameConfigDir { get; set; }
    public string GameMappingsPath { get; set; } = string.Empty;
    public string ScDataDir { get; set; }
    public string MappingDataSavePath { get; set; } = string.Empty;

    public SCFoldersForTest(string? gameConfigDir = null, string? scDataDir = null)
    {
        this.GameConfigDir = gameConfigDir ?? string.Empty;
        this.ScDataDir = scDataDir ?? string.Empty;
    }
}
