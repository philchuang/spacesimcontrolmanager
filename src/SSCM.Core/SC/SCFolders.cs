using Microsoft.Extensions.Configuration;

namespace SSCM.Core.SC;

public interface ISCFolders
{
    public string ActionMapsDir { get; }

    public string SscmDataDir { get; }
}

public class SCFolders : ISCFolders
{
    public const string PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR = @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default";
    public const string SSCM_DATA_DIR = @"SSCM\SC";

    private readonly IPlatform _platform;
    private readonly IConfiguration _config;
    private readonly string? _actionMapsDir;
    private readonly string? _sscmDataDir;

    public SCFolders(IPlatform platform, IConfiguration config)
    {
        this._platform = platform;
        this._config = config;
        var settings = this._config.GetSection("SCFolders");
        if (settings != null)
        {
            this._actionMapsDir = settings[nameof(ActionMapsDir)];
            this._sscmDataDir = settings[nameof(SscmDataDir)];
        }
    }

    public string ActionMapsDir { get { return this._actionMapsDir ?? System.IO.Path.Combine(this._platform.ProgramFilesDir, PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR); } }

    public string SscmDataDir { get { return this._sscmDataDir ?? System.IO.Path.Combine(this._platform.UserDocumentsDir, SSCM_DATA_DIR); } }
}
