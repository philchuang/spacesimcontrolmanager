using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingExporter : IMappingExporter<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath => this._folders.GameConfigPath;

    private readonly IPlatform _platform;
    private readonly IEDFolders _folders;
    
    public MappingExporter(IPlatform platform, IEDFolders folders)
    {
        this._platform = platform;
        this._folders = folders;
    }

    public string Backup()
    {
        if (!File.Exists(this.GameConfigPath))
        {
            throw new FileNotFoundException($"Could not find the Elite Dangerous mappings file at [{this.GameConfigPath}]!");
        }

        // make backup
        var backup = Path.Combine(this._folders.EliteDataDir, $"{Path.GetFileName(this.GameConfigPath)}.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        File.Copy(this.GameConfigPath, backup);
        return backup;
    }

    public string RestoreLatest()
    {
        // find all files matching pattern, sort ordinally
        var backups = Directory.GetFiles(this._folders.EliteDataDir, $"{Path.GetFileName(this.GameConfigPath)}.*.bak");
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            throw new FileNotFoundException($"Could not find any backup files in [{this._folders.EliteDataDir}]!");
        }

        // copy latest file to actionmaps.xml
        File.Copy(latest, this.GameConfigPath, true);

        return latest;
    }

    public async Task<bool> Preview(EDMappingData source)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Update(EDMappingData source)
    {
        throw new NotImplementedException();
    }

}