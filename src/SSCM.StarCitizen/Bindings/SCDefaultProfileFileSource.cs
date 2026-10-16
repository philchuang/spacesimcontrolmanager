using System.Text;
using System.Xml;
using System.Xml.Linq;
using SSCM.Core;
using static SSCM.Core.XmlExtensions;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Reads and writes <c>defaultProfile.xml</c>. This is the one place that knows where the file lives.
/// <list type="bullet">
/// <item>Reading looks in the primary directory (the working directory) first, then in the fallback
/// directory (the executable directory, where the build copies the shipped file).</item>
/// <item>Writing always goes to the primary directory, so a refresh never needs write access to the install folder.</item>
/// </list>
/// Besides the whole document (<see cref="Load"/>), it lists just the default action names (<see cref="LoadMappings"/>).
/// Files are written the way tools/Get-DefaultProfileXml.ps1 writes them:
/// two-space indent, LF line endings, no XML declaration, UTF-8 without a byte order mark.
/// </summary>
public class SCDefaultProfileFileSource
{
    public const string FileName = "defaultProfile.xml";

    private readonly ResourceFile _file;

    /// <summary>The standard locations: the working directory first, then the executable directory.</summary>
    /// <param name="executableDir">The fallback location. Defaults to the executable directory.</param>
    public static SCDefaultProfileFileSource Create(IPlatform platform, string? executableDir = null) =>
        new(platform.WorkingDir, executableDir ?? AppContext.BaseDirectory);

    /// <summary>Where <see cref="Save"/> writes, and the first place <see cref="Load"/> looks.</summary>
    public string Path => this._file.Path;

    /// <summary>The second place <see cref="Load"/> looks. Null when there is no fallback, or when it is the same file as <see cref="Path"/>.</summary>
    public string? FallbackPath => this._file.FallbackPath;

    /// <summary>The locations checked, for messages: <c>[a]</c>, or <c>[a] or [b]</c>.</summary>
    public string Searched => this._file.Searched;

    public SCDefaultProfileFileSource(string directory, string? fallbackDirectory = null)
    {
        this._file = new ResourceFile(FileName, directory, fallbackDirectory);
    }

    /// <summary>The file <see cref="Load"/> would read, or null when neither location has one.</summary>
    public string? FindExistingPath() => this._file.FindExistingPath();

    public async Task<XDocument> Load(CancellationToken cancellationToken = default)
    {
        var path = this._file.Require("Run with --refresh-defaults to extract it from the game.");

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists every named action in every named actionmap. Returns nothing when neither location has a file,
    /// so callers that only use this as a lookup keep working without one.
    /// </summary>
    public async Task<IList<SCMapping>> LoadMappings(CancellationToken cancellationToken = default)
    {
        var mappings = new List<SCMapping>();

        XDocument document;
        try
        {
            document = await this.Load(cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return mappings;
        }

        foreach (var actionMap in document.Root?.GetChildren("actionmap") ?? [])
        {
            var actionMapName = actionMap.GetAttribute("name");
            if (string.IsNullOrEmpty(actionMapName)) continue;

            foreach (var action in actionMap.GetChildren("action"))
            {
                var actionName = action.GetAttribute("name");
                if (string.IsNullOrEmpty(actionName)) continue;

                mappings.Add(new SCMapping { ActionMap = actionMapName, Action = actionName });
            }
        }

        return mappings;
    }

    /// <summary>Overwrites the file in the primary directory. The write goes to a temp file first so a failure cannot leave a partial profile behind.</summary>
    public void Save(XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            OmitXmlDeclaration = true,
        };

        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            document.Save(writer);
        }

        var directory = System.IO.Path.GetDirectoryName(this.Path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var temp = this.Path + ".tmp";
        try
        {
            File.WriteAllText(temp, builder.ToString(), new UTF8Encoding(false));
            File.Move(temp, this.Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
