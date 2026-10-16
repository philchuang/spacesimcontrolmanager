using Microsoft.Extensions.Configuration;
using SSCM.Core;

namespace SSCM.Elite;

public interface IEDFolders
{
    public string GameRoot { get; }

    public string GameConfigDir { get; }
    
    public string GameConfigPath { get; }

    public string EliteDataDir { get; }

    public string DefaultBinds { get; }

    public string DefaultBindsPath { get; }
}

public class EDFolders : IEDFolders
{
    public const string ED_DATA_DIR = "Elite";
    public const string CONTROL_SCHEMES_DIR = "ControlSchemes";
    public const string DEFAULT_BINDS = "GenericJoystick";
    public const string BINDS_EXTENSION = ".binds";
    public const string STEAM_ODYSSEY_PRODUCT_DIR = @"Steam\steamapps\common\Elite Dangerous\Products\elite-dangerous-odyssey-64";

    public string GameRoot { get; private set; }
    public string GameConfigDir { get; private set; }
    public string GameConfigPath { get; private set; }

    public string EliteDataDir { get; private set; }

    public string DefaultBinds { get; private set; }
    public string DefaultBindsFileName => DefaultBinds + BINDS_EXTENSION;
    public string DefaultBindsPath => Path.Combine(this.GameRoot, CONTROL_SCHEMES_DIR, this.DefaultBindsFileName);

    private readonly IPlatform _platform;
    private readonly ISscmFolders _sscmFolders;
    private readonly IConfiguration _config;

    public EDFolders(IPlatform platform, ISscmFolders sscmFolders, IConfiguration config)
    {
        this._platform = platform;
        this._sscmFolders = sscmFolders;
        this._config = config;

        this.GameRoot = this._config.GetValueOrNull(nameof(EDFolders), nameof(GameRoot)) ?? DefaultGameRoot();
        this.GameConfigDir = this._config.GetValueOrNull(nameof(EDFolders), nameof(GameConfigDir)) ?? FindEliteBindingsDir();
        this.GameConfigPath = new CustomBindsFile(this.GameConfigDir).Require();
        this.EliteDataDir = this._config.GetValueOrNull(nameof(EDFolders), nameof(EliteDataDir)) ?? System.IO.Path.Combine(this._sscmFolders.DataDir, ED_DATA_DIR);
        this.DefaultBinds = NormalizeBindsName(this._config.GetValueOrNull(nameof(EDFolders), nameof(DefaultBinds)) ?? DEFAULT_BINDS);
    }

    public static string DefaultGameRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), STEAM_ODYSSEY_PRODUCT_DIR);

    internal static string NormalizeBindsName(string name)
    {
        name = name.Trim();
        if (name.EndsWith(BINDS_EXTENSION, StringComparison.OrdinalIgnoreCase))
            name = name[..^BINDS_EXTENSION.Length];
        return string.IsNullOrWhiteSpace(name) ? DEFAULT_BINDS : name;
    }

    private static string FindEliteBindingsDir()
    {
        var dirs = new[] {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Frontier Developments\Elite Dangerous\Options\Bindings")
        };

        foreach (var dir in dirs)
        {
            if (new CustomBindsFile(dir).FindExistingPath() != null) return dir;
        }

        throw new DirectoryNotFoundException("Could not find Elite Dangerous user app data directory!");
    }
}
