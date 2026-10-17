using System.Text;
using SSCM.Core;
using SSCM.Elite.Bindings;

namespace SSCM.Elite;

/// <summary>
/// Prints the user's Elite Dangerous bindings as markdown, TSV, and HTML.
/// Reads the live Custom binds file (the highest <c>Custom.{major}.{minor}.binds</c> in the game bindings folder).
/// Default binds come from the ControlSchemes file named by <see cref="IEDFolders.DefaultBinds"/> (for example
/// <c>GenericJoystick.binds</c>) under <see cref="IEDFolders.GameRoot"/>. Every Primary, Secondary, and axis Binding
/// slot in that file is used. A copy of the scheme in the working directory overrides the game file. Optional
/// <c>elite-localization.ini</c> is read from the working directory first, then the executable directory.
/// Joystick-bindings.md and the HTML template are read the same way.
/// Output goes to <c>docs/elitedangerous/bindings</c> under the working directory, or to
/// <see cref="BindingPrintOptions.OutputDir"/> when that is set.
/// </summary>
public class EDBindingPrinter
{
    public const string JoystickBindingsFileName = BindingPrintIO.JoystickBindingsFileName;
    public const string HtmlTemplateFileName = "elite-dangerous-bindings.template.html";
    public const string MarkdownFileName = BindingPrintIO.MarkdownFileName;
    public const string TsvFileName = BindingPrintIO.TsvFileName;
    public const string HtmlFileName = BindingPrintIO.HtmlFileName;
    public const string JsonFileName = BindingPrintIO.JsonFileName;

    public static readonly BindingPrintFormat MarkdownFormat = BindingPrintIO.MarkdownFormat;
    public static readonly BindingPrintFormat TsvFormat = BindingPrintIO.TsvFormat;
    public static readonly BindingPrintFormat HtmlFormat = BindingPrintIO.HtmlFormat;
    public static IReadOnlyList<BindingPrintFormat> Formats => BindingPrintIO.Formats;

    public static readonly BindingPrintFlag RefreshDefaultsFlag = new(
        "refresh-defaults",
        "Would re-extract localization from the game. Elite localization extraction is not implemented yet; a warning is printed. The default ControlSchemes file is always read from GameRoot (or a copy in the current directory).");

    public static IReadOnlyList<BindingPrintFlag> Flags { get; } = [RefreshDefaultsFlag];

    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private readonly IPlatform _platform;
    private readonly IEDFolders _folders;
    private readonly IEDGameDataSource _refreshSource;
    private readonly EDLocalizationFileSource _localization;
    private readonly string _resourceDir;

    public EDBindingPrinter(IPlatform platform, IEDFolders folders, IEDGameDataSource? refreshSource = null, string? resourceDir = null)
    {
        this._platform = platform;
        this._folders = folders;
        this._refreshSource = refreshSource ?? new UnimplementedEDGameDataSource();
        this._resourceDir = resourceDir ?? AppContext.BaseDirectory;
        this._localization = EDLocalizationFileSource.Create(platform, this._resourceDir);
    }

    public string OutputDir => Path.Combine(this._platform.WorkingDir, "docs", "elitedangerous", "bindings");

    public async Task<BindingPrintResult> Print(string? format = null, BindingPrintOptions? printOptions = null, CancellationToken cancellationToken = default)
    {
        var selected = BindingPrintIO.ResolveFormat(format, Formats);
        bool Wants(BindingPrintFormat candidate) => selected == null || selected == candidate;
        var outputDir = BindingPrintIO.ResolveOutputDir(this._platform, this.OutputDir, printOptions);

        this.EnsureBindsFile();
        if (printOptions?.Has(RefreshDefaultsFlag.Name) == true) await this.RefreshDefaults(cancellationToken).ConfigureAwait(false);

        var document = await this.BuildDocument(cancellationToken).ConfigureAwait(false);
        var written = new List<string>();

        if (Wants(MarkdownFormat))
        {
            var content = BindingDocumentRenderer.RenderMarkdown(document, "Elite Dangerous Bindings");
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
            written.Add(this.Write(outputDir, JsonFileName, BindingDocumentRenderer.RenderJson(document), $"{document.Actions.Count} JSON actions"));
        }

        return new BindingPrintResult { ActionCount = document.Actions.Count, WrittenPaths = written };
    }

    /// <summary>
    /// Placeholder for extracting localization from the game.
    /// Warns and continues; ControlSchemes defaults are still read from GameRoot (or the working directory).
    /// </summary>
    public Task RefreshDefaults(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = this._refreshSource;
        this.WarningOutput(
            $"{UnimplementedEDGameDataSource.Message} Default binds are read from [{this._folders.DefaultBindsPath}]. Place {EDLocalizationFileSource.FileName} in the current directory when you have it.");
        return Task.CompletedTask;
    }

    public async Task<BindingDocument> BuildDocument(CancellationToken cancellationToken = default)
    {
        var bindsPath = this.EnsureBindsFile();

        this.DebugOutput($"Reading Elite binds [{bindsPath}]...");
        var custom = await this.CreateImporter(bindsPath).Read().ConfigureAwait(false);

        var defaults = await this.LoadScheme(cancellationToken).ConfigureAwait(false);
        var localization = await this.LoadLocalization(cancellationToken).ConfigureAwait(false);
        var actions = EDBindingCatalogBuilder.Build(custom, defaults, localization);

        EDJoystickDescriptions.Apply(actions, await this.LoadJoystickDescriptions(custom, cancellationToken).ConfigureAwait(false));

        var document = new BindingDocument { GeneratedAt = BindingPrintIO.CurrentTimestamp(this._platform), Actions = actions };
        EDBindingDisplay.Prepare(document);
        return document;
    }

    private async Task<EDMappingData?> LoadScheme(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var fileName = Path.GetFileName(this._folders.DefaultBindsPath);
        var workingPath = Path.Combine(this._platform.WorkingDir, fileName);
        var gamePath = this._folders.DefaultBindsPath;
        var path = File.Exists(workingPath) ? workingPath : File.Exists(gamePath) ? gamePath : null;
        if (path == null)
        {
            if (!Directory.Exists(this._folders.GameRoot))
            {
                // The product folder is the install. A wrong path used to warn and print empty defaults; that hid an Epic or Frontier install sitting at another path.
                throw new SscmException(
                    $"Elite Dangerous was not found at [{this._folders.GameRoot}]. The default is the Steam Odyssey product folder, so an Epic or Frontier install will not match. Set EDFolders:GameRoot in appsettings.local.json to your product folder, or place {fileName} in the current directory.");
            }

            this.WarningOutput($"Could not find {fileName} at [{workingPath}] or [{gamePath}]. Default bind columns will be empty. Set EDFolders:GameRoot and EDFolders:DefaultBinds in appsettings.local.json if your Elite Dangerous product folder or scheme name is different.");
            return null;
        }

        this.DebugOutput($"Reading default scheme [{path}]...");
        return await this.CreateImporter(path).Read().ConfigureAwait(false);
    }

    private async Task<Dictionary<string, string>?> LoadLocalization(CancellationToken cancellationToken)
    {
        var path = this._localization.FindExistingPath();
        if (path == null) return null;

        this.DebugOutput($"Reading localization [{path}]...");
        return await this._localization.Load(cancellationToken).ConfigureAwait(false);
    }

    private async Task<JoystickDescriptions> LoadJoystickDescriptions(EDMappingData custom, CancellationToken cancellationToken)
    {
        var descriptions = await JoystickBindingsFile.Load(this._platform, this._resourceDir, this.WarningOutput, this.DebugOutput, cancellationToken).ConfigureAwait(false);
        EDJoystickDescriptions.WarnUnattached(descriptions, EDJoystickDescriptions.LoadAttachedDeviceIds(custom), this.WarningOutput);
        return descriptions;
    }

    private string EnsureBindsFile()
    {
        string path;
        try
        {
            path = this._folders.GameConfigPath;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new SscmException($"{ex.Message} Start Elite Dangerous once so it creates your Custom binds file.", ex);
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new SscmException($"Could not find the Elite Dangerous bindings file at [{path}]. Start the game once so it creates a {CustomBindsFile.FileNameFormat} file.");
        }

        return path;
    }

    private MappingImporter CreateImporter(string gameConfigPath)
    {
        var importer = new MappingImporter(this._platform, gameConfigPath, this._resourceDir);
        importer.StandardOutput += msg => this.StandardOutput(msg);
        importer.WarningOutput += msg => this.WarningOutput(msg);
        importer.DebugOutput += msg => this.DebugOutput(msg);
        return importer;
    }

    private string Write(string outputDir, string fileName, string content, string description) =>
        BindingPrintIO.Write(outputDir, fileName, content, description, msg => this.StandardOutput(msg));

    private ResourceFile Resource(string fileName) =>
        ResourceFile.Create(this._platform, fileName, this._resourceDir);
}
