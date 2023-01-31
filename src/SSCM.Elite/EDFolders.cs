using Microsoft.Extensions.Configuration;
using SSCM.Core;

namespace SSCM.Elite;

public interface IEDFolders
{
    public string GameConfigDir { get; }
    
    public string GameConfigPath { get; }

    public string EliteDataDir { get; }
}

public class EDFolders : IEDFolders
{
    public const string ED_DATA_DIR = "Elite";

    private static string[] BINDS = { "Custom.4.0.binds", "Custom.3.0.binds" };

    public string GameConfigDir { get; private set; }
    public string GameConfigPath { get; private set; }

    public string EliteDataDir { get; private set; }

    private readonly IPlatform _platform;
    private readonly ISscmFolders _sscmFolders;
    private readonly IConfiguration _config;

    public EDFolders(IPlatform platform, ISscmFolders sscmFolders, IConfiguration config)
    {
        this._platform = platform;
        this._sscmFolders = sscmFolders;
        this._config = config;

        this.GameConfigDir = this._config.GetValueOrNull(nameof(EDFolders), nameof(GameConfigDir)) ?? FindEliteBindingsDir();
        this.GameConfigPath = BINDS.Select(f => Path.Combine(this.GameConfigDir, f)).FirstOrDefault(p => File.Exists(p)) ?? throw new FileNotFoundException($"Could not find .binds file in [{this.GameConfigDir}]!");
        this.EliteDataDir = this._config.GetValueOrNull(nameof(EDFolders), nameof(EliteDataDir)) ?? System.IO.Path.Combine(this._sscmFolders.DataDir, ED_DATA_DIR);
    }

    private string FindEliteBindingsDir()
    {
        var dirs = new[] { 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Frontier Developments\Elite Dangerous\Options\Bindings")
        };

        foreach (var dir in dirs)
        {
            if (Directory.Exists(dir))
            {
                var path = Path.Combine(dir, "Custom.4.0.binds");
                if (File.Exists(path)) return dir;
                path = Path.Combine(dir, "Custom.3.0.binds");
                if (File.Exists(path)) return dir;
            }
        }
        
        throw new DirectoryNotFoundException("Could not find Elite Dangerous user app data directory!");
    }
}
