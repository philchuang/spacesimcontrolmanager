using System.Text;
using System.Xml.Linq;
using SSCM.Core;
using SSCM.StarCitizen.Bindings;

namespace SSCM.StarCitizen;

/// <summary>
/// Prints the user's Star Citizen bindings as markdown, TSV, and HTML.
/// Reads the selected environment's actionmaps.xml and, if it exists, its global.ini localization (labels are marked
/// /FNF when it is missing, /KNF when a key is missing). The game's own global.ini is always used when it exists. Only when the game
/// has none is the copy in the working directory used. The tool also reads three files that ship with it:
/// defaultProfile.xml, joystick-bindings.md, and the HTML template. Each is read from the working directory
/// first, so a copy placed there overrides the shipped one, then from the executable directory.
/// With the <see cref="RefreshDefaultsFlag"/> switched on, defaultProfile.xml is first extracted from the
/// environment's Data.p4k and saved to the working directory. If the game has no global.ini, that is extracted in the
/// same pass and saved to the working directory as well.
/// Output goes to <c>docs/starcitizen/bindings</c> under the working directory, or to <see cref="BindingPrintOptions.OutputDir"/> when that is set.
/// </summary>
public class SCBindingPrinter
{
    public const string JoystickBindingsFileName = BindingPrintIO.JoystickBindingsFileName;
    public const string HtmlTemplateFileName = "star-citizen-bindings.template.html";
    public const string MarkdownFileName = BindingPrintIO.MarkdownFileName;
    public const string TsvFileName = BindingPrintIO.TsvFileName;
    public const string HtmlFileName = BindingPrintIO.HtmlFileName;
    public const string JsonFileName = BindingPrintIO.JsonFileName;

    public static readonly BindingPrintFormat MarkdownFormat = BindingPrintIO.MarkdownFormat;
    public static readonly BindingPrintFormat TsvFormat = BindingPrintIO.TsvFormat;
    public static readonly BindingPrintFormat HtmlFormat = BindingPrintIO.HtmlFormat;

    /// <summary>Everything this generator can print on its own, in the order they are written.</summary>
    public static IReadOnlyList<BindingPrintFormat> Formats => BindingPrintIO.Formats;

    public static readonly BindingPrintFlag RefreshDefaultsFlag = new(
        "refresh-defaults",
        "Re-extract the default profile from the game files and save it as defaultProfile.xml in the current directory, before printing. A missing global.ini is extracted too.");

    public static IReadOnlyList<BindingPrintFlag> Flags { get; } = [RefreshDefaultsFlag];

    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;
    private readonly ISCGameDataSource _refreshSource;
    private readonly SCDefaultProfileFileSource _defaultProfile;
    private readonly SCLocalizationFileSource _localization;
    private readonly string _resourceDir;

    /// <param name="refreshSource">Where --refresh-defaults extracts the default profile and localization from. Defaults to the environment's Data.p4k.</param>
    /// <param name="resourceDir">The fallback location for defaultProfile.xml, joystick-bindings.md, and the HTML template. Defaults to the executable directory.</param>
    public SCBindingPrinter(IPlatform platform, ISCFolders folders, ISCGameDataSource? refreshSource = null, string? resourceDir = null)
    {
        this._platform = platform;
        this._folders = folders;
        this._refreshSource = refreshSource ?? new SCP4kGameDataSource(folders);
        this._resourceDir = resourceDir ?? AppContext.BaseDirectory;
        this._defaultProfile = SCDefaultProfileFileSource.Create(platform, this._resourceDir);
        this._localization = SCLocalizationFileSource.Create(platform, folders);
    }

    /// <summary>Where files are written when <see cref="BindingPrintOptions.OutputDir"/> is not set.</summary>
    public string OutputDir => Path.Combine(this._platform.WorkingDir, "docs", "starcitizen", "bindings");

    /// <param name="format">The <see cref="BindingPrintFormat.Name"/> to print, or null for every format.</param>
    public async Task<BindingPrintResult> Print(string? format = null, BindingPrintOptions? printOptions = null, CancellationToken cancellationToken = default)
    {
        var selected = BindingPrintIO.ResolveFormat(format, Formats);
        bool Wants(BindingPrintFormat candidate) => selected == null || selected == candidate;
        var outputDir = BindingPrintIO.ResolveOutputDir(this._platform, this.OutputDir, printOptions);

        this.EnsureEnvironmentExists();
        if (printOptions?.Has(RefreshDefaultsFlag.Name) == true) await this.RefreshDefaults(cancellationToken).ConfigureAwait(false);

        var document = await this.BuildDocument(cancellationToken).ConfigureAwait(false);
        var written = new List<string>();

        if (Wants(MarkdownFormat))
        {
            var content = BindingDocumentRenderer.RenderMarkdown(document, "Star Citizen Bindings");
            written.Add(this.Write(outputDir, MarkdownFileName, content, $"{BindingDocumentRenderer.CountRows(document, true)} markdown rows"));
        }

        if (Wants(TsvFormat))
        {
            var content = BindingDocumentRenderer.RenderTsv(document);
            written.Add(this.Write(outputDir, TsvFileName, content, $"{BindingDocumentRenderer.CountRows(document, false)} TSV rows"));
        }

        if (Wants(HtmlFormat))
        {
            var template = await File.ReadAllTextAsync(this.Resource(HtmlTemplateFileName).Require(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            var content = BindingDocumentRenderer.RenderHtml(document, template);
            written.Add(this.Write(outputDir, HtmlFileName, content, $"{document.Actions.Count} HTML actions"));

            // The HTML embeds this data, so the complete source document is written next to it.
            written.Add(this.Write(outputDir, JsonFileName, BindingDocumentRenderer.RenderJson(document), $"{document.Actions.Count} JSON actions"));
        }

        return new BindingPrintResult { ActionCount = document.Actions.Count, WrittenPaths = written };
    }

    /// <summary>
    /// Extracts the default profile for the selected environment and saves it as defaultProfile.xml in the working directory.
    /// When the game's own global.ini is missing, it is extracted from the same archive pass and saved to the working directory too.
    /// </summary>
    public async Task RefreshDefaults(CancellationToken cancellationToken = default)
    {
        this.EnsureEnvironmentExists();

        var needsLocalization = this._localization.NeedsExtractionFromArchive;
        this.DebugOutput(needsLocalization
            ? $"Extracting default profile and localization from the [{this._folders.Environment}] Data.p4k..."
            : $"Extracting default profile from the [{this._folders.Environment}] Data.p4k (localization already exists at [{this._localization.GamePath}])...");

        var data = await this._refreshSource.Extract(needsLocalization, cancellationToken).ConfigureAwait(false);
        this._defaultProfile.Save(data.DefaultProfile);
        this.StandardOutput($"Refreshed default profile [{this._defaultProfile.Path}] from [{this._folders.Environment}]");

        if (!needsLocalization) return;

        if (data.Localization == null)
        {
            this.WarningOutput($"The [{this._folders.Environment}] Data.p4k does not contain [{SCP4kGameDataSource.LocalizationEntryName}], so no localization file was saved.");
            return;
        }

        this._localization.Save(data.Localization);
        this.StandardOutput($"Extracted localization [{this._localization.WorkingPath}] from [{this._folders.Environment}]");
    }

    /// <summary>Builds the in-memory document: catalog, localization, rebinds, then joystick descriptions.</summary>
    public async Task<BindingDocument> BuildDocument(CancellationToken cancellationToken = default)
    {
        this.EnsureEnvironmentExists();
        var actionMapsPath = this.RequireFile(this.GameMappingsPath, "Star Citizen actionmaps.xml");

        this.DebugOutput($"Reading default profile [{this._defaultProfile.FindExistingPath() ?? this._defaultProfile.Path}]...");
        var defaultProfile = await this._defaultProfile.Load(cancellationToken).ConfigureAwait(false);

        var localization = await this.LoadLocalizationMap(cancellationToken).ConfigureAwait(false);

        this.DebugOutput($"Reading actionmaps [{actionMapsPath}]...");
        var actionMaps = await this.CreateImporter().ReadActionMaps(cancellationToken).ConfigureAwait(false);

        var actions = BindingCatalogBuilder.Build(defaultProfile, localization, actionMaps);

        var instances = SCJoystickDescriptions.LoadJoystickInstances(actionMaps.Inputs);
        SCJoystickDescriptions.Apply(actions, await this.LoadJoystickDescriptions(instances, cancellationToken).ConfigureAwait(false), instances);

        return new BindingDocument { GeneratedAt = BindingPrintIO.CurrentTimestamp(this._platform), Actions = actions };
    }

    private async Task<Dictionary<string, string>?> LoadLocalizationMap(CancellationToken cancellationToken)
    {
        var path = this._localization.FindExistingPath();
        if (path == null)
        {
            this.WarningOutput(
                $"Could not find the Star Citizen localization file at {this._localization.Searched}. " +
                $"Localized @ keys will be written with a {BindingCatalogBuilder.FileNotFoundSuffix} suffix. Run with --{RefreshDefaultsFlag.Name} to extract it from the game.");
            return null;
        }

        this.DebugOutput($"Reading localization [{path}]...");
        return await this._localization.Load(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>joystick-bindings.md is optional. Without it, bound joystick inputs are described as {NOT_FOUND}.</summary>
    private async Task<JoystickDescriptions> LoadJoystickDescriptions(IReadOnlyDictionary<string, string> joystickInstances, CancellationToken cancellationToken)
    {
        var descriptions = await JoystickBindingsFile.Load(this._platform, this._resourceDir, this.WarningOutput, this.DebugOutput, cancellationToken).ConfigureAwait(false);
        SCJoystickDescriptions.WarnUnattached(descriptions, joystickInstances, this.WarningOutput);
        return descriptions;
    }

    private MappingImporter CreateImporter()
    {
        var importer = new MappingImporter(this._platform, this._folders);
        importer.StandardOutput += msg => this.StandardOutput(msg);
        importer.WarningOutput += msg => this.WarningOutput(msg);
        importer.DebugOutput += msg => this.DebugOutput(msg);
        return importer;
    }

    private string GameMappingsPath
    {
        get
        {
            try
            {
                return this._folders.GameMappingsPath;
            }
            catch (DirectoryNotFoundException ex)
            {
                // The environment is installed, but the game has never created settings for it.
                throw new SscmException($"{ex.Message} Start Star Citizen once so it creates your settings.", ex);
            }
        }
    }

    /// <summary>Throws a message suitable for end users when the selected environment is not installed.</summary>
    private void EnsureEnvironmentExists()
    {
        var environmentDir = this._folders.GameEnvironmentDir;
        if (Directory.Exists(environmentDir)) return;

        var environment = this._folders.Environment.ToUpperInvariant();
        var root = string.IsNullOrEmpty(environmentDir) ? null : Directory.GetParent(environmentDir)?.FullName;
        if (root == null || !Directory.Exists(root))
        {
            throw new SscmException(
                $"Star Citizen was not found at [{root}]. Set SCFolders:GameRoot in appsettings.local.json to your Star Citizen install folder.");
        }

        // An installed environment is a folder that holds the game archive.
        var installed = Directory.GetDirectories(root)
            .Where(dir => File.Exists(Path.Combine(dir, SCP4kGameDataSource.P4kFileName)))
            .Select(dir => Path.GetFileName(dir))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var message = $"Star Citizen environment [{environment}] was not found at [{environmentDir}].";
        message += installed.Count > 0
            ? $" Installed environments: {string.Join(", ", installed)}. Use --environment to choose one."
            : $" No installed environments were found in [{root}].";
        throw new SscmException(message);
    }

    private string Write(string outputDir, string fileName, string content, string description) =>
        BindingPrintIO.Write(outputDir, fileName, content, description, msg => this.StandardOutput(msg));

    /// <summary>A file that ships with the tool: the working directory is checked first, then the executable directory.</summary>
    private ResourceFile Resource(string fileName) =>
        ResourceFile.Create(this._platform, fileName, this._resourceDir);

    private string RequireFile(string path, string description)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Could not find {description} [{path}].", path);
        return path;
    }
}
