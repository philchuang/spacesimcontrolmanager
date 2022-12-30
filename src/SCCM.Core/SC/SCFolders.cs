namespace SCCM.Core.SC;

public interface ISCFolders
{
    public string ActionMapsDir { get; }

    public string SccmDir { get; }
}

public class SCFolders : ISCFolders
{
    private readonly IPlatform _platform;

    public SCFolders(IPlatform platform)
    {
        this._platform = platform;
    }

    public string ActionMapsDir { get { return System.IO.Path.Combine(this._platform.ProgramFilesDir, @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default"); } }

    public string SccmDir { get { return System.IO.Path.Combine(this._platform.UserDocumentsDir, "SCCM"); } }
}
