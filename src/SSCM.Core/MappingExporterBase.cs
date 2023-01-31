using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Core;

public abstract class MappingExporterBase<TData> : IMappingExporter<TData>
{
    public event Action<string> StandardOutput = delegate {};
    protected void _StandardOutput(string s) => this.StandardOutput(s);
    public event Action<string> WarningOutput = delegate {};
    protected void _WarningOutput(string s) => this.WarningOutput(s);
    public event Action<string> DebugOutput = delegate {};
    protected void _DebugOutput(string s) => this.DebugOutput(s);

    protected readonly IPlatform _platform;
    
    protected MappingExporterBase(IPlatform platform)
    {
        this._platform = platform;
    }

    public abstract string Backup();

    protected string Backup(string target, string backupDir)
    {
        if (!File.Exists(target))
        {
            throw new FileNotFoundException($"Could not find file [{target}]!");
        }

        // make backup
        Directory.CreateDirectory(backupDir);
        var name = new FileInfo(target).Name;
        var backupPath = Path.Combine(backupDir, $"{name}.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        File.Copy(target, backupPath);
        return backupPath;
    }

    public abstract string RestoreLatest();

    protected string RestoreLatest(string backupDir, string filter, string targetPath)
    {
        // find all files matching pattern, sort ordinally
        var backups = Directory.GetFiles(backupDir, filter);
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            throw new FileNotFoundException($"Could not find any backup files in [{backupDir}]!");
        }

        // copy latest file
        File.Copy(latest, targetPath, true);

        return latest;
    }

    public abstract Task<bool> Preview(TData source);

    public abstract Task<bool> Update(TData source);
}