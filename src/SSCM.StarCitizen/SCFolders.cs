using Microsoft.Extensions.Configuration;
using SSCM.Core;

namespace SSCM.StarCitizen;

public interface ISCFolders
{
    public string GameConfigDir { get; }

    public string ScDataDir { get; }
}

public class SCFolders : ISCFolders
{
    public const string PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR = @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default";
    public const string SSCM_DATA_DIR = "SC";

    public string GameConfigDir { get; private set; }

    public string ScDataDir { get; private set; }

    private readonly IPlatform _platform;
    private readonly ISscmFolders _sscmFolders;
    private readonly IConfiguration _config;

    public SCFolders(IPlatform platform, ISscmFolders sscmFolders, IConfiguration config)
    {
        this._platform = platform;
        this._sscmFolders = sscmFolders;
        this._config = config;

        this.GameConfigDir = this._config.GetValueOrNull(nameof(SCFolders), nameof(GameConfigDir)) ?? System.IO.Path.Combine(this._platform.ProgramFilesDir, PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR);
        this.ScDataDir = this._config.GetValueOrNull(nameof(SCFolders), nameof(ScDataDir)) ?? System.IO.Path.Combine(this._sscmFolders.DataDir, SSCM_DATA_DIR);
    }
}
