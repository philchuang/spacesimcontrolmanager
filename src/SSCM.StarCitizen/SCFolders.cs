using Microsoft.Extensions.Configuration;
using SSCM.Core;

namespace SSCM.StarCitizen;

public interface ISCFolders
{

    public string GameAttributesPath { get; }

    public string GameConfigDir { get; }

    public string GameMappingsPath { get; }

    public string ScDataDir { get; }

    public string MappingDataSavePath { get; }
}

public class SCFolders : ISCFolders
{
    public const string PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR = @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default";
    public const string SC_DATA_DIR = "SC";

    public string GameConfigDir { get; init; }

    public string GameAttributesPath => Path.Combine(this.GameConfigDir, Constants.SC_ATTRIBUTES_XML_NAME);
    public string GameMappingsPath => Path.Combine(this.GameConfigDir, Constants.SC_ACTIONMAPS_XML_NAME);

    public string ScDataDir { get; init; }

    public string MappingDataSavePath => Path.Combine(this.ScDataDir, Constants.SSCM_SCMAPPINGS_JSON_NAME);

    private readonly IPlatform _platform;
    private readonly ISscmFolders _sscmFolders;
    private readonly IConfiguration _config;

    public SCFolders(IPlatform platform, ISscmFolders sscmFolders, IConfiguration config)
    {
        this._platform = platform;
        this._sscmFolders = sscmFolders;
        this._config = config;

        this.GameConfigDir = this._config.GetValueOrNull(nameof(SCFolders), nameof(GameConfigDir)) ?? Path.Combine(this._platform.ProgramFilesDir, PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR);
        this.ScDataDir = this._config.GetValueOrNull(nameof(SCFolders), nameof(ScDataDir)) ?? Path.Combine(this._sscmFolders.DataDir, SC_DATA_DIR);
    }
}
