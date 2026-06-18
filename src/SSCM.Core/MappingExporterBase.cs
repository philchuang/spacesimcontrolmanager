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

    public ExportOptions ExportOptions { get; set; }

    protected readonly IPlatform _platform;
    
    protected MappingExporterBase(IPlatform platform)
    {
        this._platform = platform;
        this.ExportOptions = ExportOptions.Default;
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

    public virtual async Task<bool> UpdateInteractive(TData source, IUserInput userInput)
    {
        var session = await this.CreateInteractiveSession(source);
        if (!session.HasRows) return false;

        if (!userInput.YesNo("\nStart interactive export?"))
        {
            throw new UserInputCancelledException();
        }

        var changed = false;
        foreach (var row in session.Rows)
        {
            var currentValue = string.IsNullOrWhiteSpace(row.CurrentValue) ? "<none>" : row.CurrentValue;
            var newValue = string.IsNullOrWhiteSpace(row.NewValue) ? "<none>" : row.NewValue;
            if (userInput.YesNo($"{row.ChangeKind} [{row.ItemId}] {currentValue} => {newValue} ?"))
            {
                changed |= row.Apply();
            }
        }

        if (!changed) return false;

        if (!userInput.YesNo("\nFinish interactive export and save changes?"))
        {
            throw new UserInputCancelledException();
        }

        await this.SaveInteractive();
        return true;
    }

    public abstract Task<InteractiveChangeSession> CreateInteractiveSession(TData source);

    public abstract Task SaveInteractive();
}
