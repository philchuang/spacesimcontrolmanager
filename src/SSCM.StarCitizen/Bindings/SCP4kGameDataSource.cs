using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core.P4k;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Reads <c>defaultProfile.xml</c> (and optionally <c>global.ini</c>) straight out of the selected environment's <c>Data.p4k</c>.
/// This mirrors tools/Get-DefaultProfileXml.ps1: extract the entry, convert the binary XML
/// (unforge), strip unforge's <c>&lt;![CDATA[ /&gt; ]]&gt;</c> artifacts, then reload without
/// insignificant whitespace. The script's final re-indent is applied when the result is written by
/// <see cref="SCDefaultProfileFileSource"/>, since it only affects how text is written.
/// The result starts with a comment naming the environment and the archive's modification date.
/// </summary>
public partial class SCP4kGameDataSource : ISCGameDataSource
{
    public const string P4kFileName = "Data.p4k";
    public const string EntryName = "Data/Libs/Config/defaultProfile.xml";
    public const string LocalizationEntryName = "Data/Localization/english/global.ini";

    // Same pattern the script applies to the CDATA body: optional whitespace, '>' or '/>', optional whitespace.
    [GeneratedRegex(@"^\s*/?>\s*$")]
    private static partial Regex UnforgeArtifactRegex();

    private readonly ISCFolders _folders;

    public SCP4kGameDataSource(ISCFolders folders)
    {
        this._folders = folders;
    }

    /// <summary>Extracts only the default profile. Use <see cref="Extract"/> when localization may be needed too.</summary>
    public async Task<XDocument> Load(CancellationToken cancellationToken = default) =>
        (await this.Extract(includeLocalization: false, cancellationToken).ConfigureAwait(false)).DefaultProfile;

    /// <summary>The profile is required. The localization file is optional, so a missing entry gives a null <see cref="SCGameData.Localization"/>.</summary>
    public async Task<SCGameData> Extract(bool includeLocalization, CancellationToken cancellationToken = default)
    {
        var p4kPath = Path.Combine(this._folders.GameEnvironmentDir, P4kFileName);
        if (!File.Exists(p4kPath)) throw new FileNotFoundException($"Could not find the Star Citizen data archive [{p4kPath}].", p4kPath);

        // One scan of the archive's directory serves every entry.
        var entryNames = includeLocalization ? new[] { EntryName, LocalizationEntryName } : new[] { EntryName };
        var entries = await Task.Run(() => P4kArchive.ReadEntries(p4kPath, entryNames, cancellationToken), cancellationToken).ConfigureAwait(false);

        if (!entries.TryGetValue(EntryName, out var profileData))
        {
            throw new FileNotFoundException($"Entry [{EntryName}] was not found in the archive.", EntryName);
        }

        var document = Normalize(CryXmlConverter.ToXDocument(profileData));

        // Record where this copy came from, so a stale included profile is easy to spot.
        var archiveDate = File.GetLastWriteTime(p4kPath).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        document.AddFirst(new XComment($" extracted from {this._folders.Environment.ToUpperInvariant()} {P4kFileName} dated {archiveDate} "));

        entries.TryGetValue(LocalizationEntryName, out var localization);
        return new SCGameData(document, localization);
    }

    /// <summary>Applies the script's post-processing to a converted profile, in place, and returns it.</summary>
    public static XDocument Normalize(XDocument document)
    {
        // Remove the stray "/>" content unforge leaves behind (the script's CDATA regex replacement).
        foreach (var text in document.DescendantNodes().OfType<XText>().Where(t => UnforgeArtifactRegex().IsMatch(t.Value)).ToList())
        {
            text.Remove();
        }

        // XmlDocument.PreserveWhitespace = false drops whitespace-only text, but keeps CDATA.
        foreach (var text in document.DescendantNodes().OfType<XText>().Where(t => t is not XCData && IsXmlWhitespace(t.Value)).ToList())
        {
            text.Remove();
        }

        return document;
    }

    private static bool IsXmlWhitespace(string value) => value.All(c => c is ' ' or '\t' or '\r' or '\n');
}
