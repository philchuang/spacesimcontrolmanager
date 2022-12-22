namespace SCCM.Core;

public interface IFolders
{
    public string ActionMapsDir { get; }

    public string SccmDir { get; }
}

public class Folders : IFolders
{
    private readonly IPlatform _platform;

    public Folders(IPlatform platform)
    {
        this._platform = platform;
    }

    public string ActionMapsDir { get { return System.IO.Path.Combine(this._platform.ProgramFilesDir, @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default"); } }

    public string SccmDir { get { return System.IO.Path.Combine(this._platform.UserDocumentsDir, "SCCM"); } }
}
