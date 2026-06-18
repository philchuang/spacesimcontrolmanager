namespace SSCM.Core;

public class ExportOptions {
    public static ExportOptions Default = new ExportOptions();

    /// <summary>
    /// Whether or not to only export settings that already exist in the game mappings.
    /// </summary>
    public bool OnlyMatches { get; set; } = false;

    public ExportOptions()
    {
    }
}