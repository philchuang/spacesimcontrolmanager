using Microsoft.Extensions.Configuration;

namespace SSCM.Core;

public interface ISscmFolders
{
    string DataDir { get; }
}

public class SscmFolders : ISscmFolders
{
    public const string DATA_DIR_NAME = "SSCM";

    public string DataDir { get; private set; }

    private readonly IPlatform _platform;
    private readonly IConfiguration _config;

    public SscmFolders(IPlatform platform, IConfiguration config)
    {
        this._platform = platform;
        this._config = config;
        this.DataDir = this._config.GetValueOrNull(nameof(SscmFolders), nameof(DataDir)) ?? Path.Combine(this._platform.UserDocumentsDir, DATA_DIR_NAME);
    }
}
