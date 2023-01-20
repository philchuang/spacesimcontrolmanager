using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingDataRepository : IMappingDataRepository<MappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};


    public string MappingDataSavePath { get; set; }
    public string BackupFilenameFormat { get; set;}

    private readonly IPlatform _platform;
    private readonly string _mappingDataSaveDir;

    public MappingDataRepository(IPlatform platform, string mappingDataSavePath, string backupFilenameFormat)
    {
        this._platform = platform;
        this.MappingDataSavePath = mappingDataSavePath;
        this._mappingDataSaveDir = new FileInfo(this.MappingDataSavePath).DirectoryName;
        this.BackupFilenameFormat = backupFilenameFormat;
    }

    public MappingData CreateNew() => new MappingData();

    public async Task<MappingData?> Load(string? saveFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            saveFilePath = this.MappingDataSavePath;
        }

        var serializer = new DataSerializer<MappingData>(saveFilePath);
        return await serializer.Read();
    }

    public async Task Save(MappingData data, string? saveFilePath = null)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            saveFilePath = this.MappingDataSavePath;
        }

        var saveDir = new FileInfo(saveFilePath).DirectoryName;
        System.IO.Directory.CreateDirectory(saveDir);
        var serializer = new DataSerializer<MappingData>(saveFilePath);
        await serializer.Write(data);
        this.StandardOutput($"Mappings saved to [{this.MappingDataSavePath}].");
    }

    public string? Backup()
    {
        if (!System.IO.File.Exists(this.MappingDataSavePath))
        {
            this.WarningOutput($"Nothing to back up at [{this.MappingDataSavePath}].");
            return null;
        }

        // make backup of actionmaps.xml
        var backupPath = System.IO.Path.Combine(this._mappingDataSaveDir, string.Format(this.BackupFilenameFormat, this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")));
        System.IO.File.Copy(this.MappingDataSavePath, backupPath);
        return backupPath;
    }

    public string? RestoreLatest()
    {
        // find all files matching pattern, sort ordinally
        var backups = System.IO.Directory.GetFiles(this._mappingDataSaveDir, string.Format(this.BackupFilenameFormat, "*"));
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            this.WarningOutput($"Could not find any backup files in [{this._mappingDataSaveDir}].");
            return null;
        }

        // copy latest file to actionmaps.xml
        System.IO.File.Copy(latest, this.MappingDataSavePath, true);

        return latest;
    }
}