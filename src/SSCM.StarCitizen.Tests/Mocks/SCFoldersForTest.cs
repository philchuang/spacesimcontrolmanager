using SSCM.StarCitizen;

namespace SSCM.StarCitizen.Tests.Mocks;

public class SCFoldersForTest : ISCFolders
{
    public string GameAttributesPath { get; set; }
    public string GameConfigDir { get; set; }
    public string GameMappingsPath { get; set; }
    public string ScDataDir { get; set; }
    public string MappingDataSavePath { get; set; }

    public SCFoldersForTest(string? gameConfigDir = null, string? scDataDir = null)
    {
        this.GameConfigDir = gameConfigDir ?? string.Empty;
        this.ScDataDir = scDataDir ?? string.Empty;
    }
}