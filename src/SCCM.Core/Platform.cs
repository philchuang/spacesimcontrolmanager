using System.Windows;

namespace SCCM.Core;

public interface IPlatform
{
    public DateTime UtcNow { get; }
    public string ProgramFilesDir { get; }
    public string UserDocumentsDir { get; }

    public void Open(string path);
}

public class Platform : IPlatform
{
    public DateTime UtcNow { get { return DateTime.UtcNow; } }
    public string ProgramFilesDir { get { return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles); } }
    public string UserDocumentsDir { get { return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments); } }
    
    public void Open(string path)
    {
        new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            }
        }.Start();
    }
}
