namespace SSCM.Elite.Tests.Mocks;

public class EDFoldersForTest : IEDFolders
{
    public string GameConfigDir { get; set; } = string.Empty;
    public string GameConfigPath { get; set; } = string.Empty;
    public string EliteDataDir { get; set; } = string.Empty;

    public EDFoldersForTest()
    {
    }
}