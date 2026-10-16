namespace SSCM.Elite.Tests.Mocks;

public class EDFoldersForTest : IEDFolders
{
    public string GameRoot { get; set; } = string.Empty;
    public string GameConfigDir { get; set; } = string.Empty;
    public string GameConfigPath { get; set; } = string.Empty;
    public string EliteDataDir { get; set; } = string.Empty;
    public string DefaultBinds { get; set; } = EDFolders.DEFAULT_BINDS;
    public string DefaultBindsPath => Path.Combine(this.GameRoot, EDFolders.CONTROL_SCHEMES_DIR, this.DefaultBinds + EDFolders.BINDS_EXTENSION);
}
