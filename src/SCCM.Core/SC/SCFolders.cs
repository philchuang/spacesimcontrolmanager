using Microsoft.Extensions.Configuration;

namespace SCCM.Core.SC;

public interface ISCFolders
{
    public string ActionMapsDir { get; }

    public string SccmDir { get; }
}

public class SCFolders : ISCFolders
{
    public const string PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR = @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default";
    public const string SCCM_DIR = "SCCM";

    private readonly IPlatform _platform;
    private readonly IConfiguration _config;
    private readonly string? _actionMapsDir;
    private readonly string? _sccmDir;

    public SCFolders(IPlatform platform, IConfiguration config)
    {
        this._platform = platform;
        this._config = config;
        var settings = this._config.GetSection("SCFolders");
        if (settings != null)
        {
            this._actionMapsDir = settings[nameof(ActionMapsDir)];
            this._sccmDir = settings[nameof(SccmDir)];
        }
    }

    public string ActionMapsDir { get { return this._actionMapsDir ?? System.IO.Path.Combine(this._platform.ProgramFilesDir, PROGRAM_FILES_SC_PROFILES_DEFAULT_DIR); } }

    public string SccmDir { get { return this._sccmDir ?? System.IO.Path.Combine(this._platform.UserDocumentsDir, SCCM_DIR); } }
}
