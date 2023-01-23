using System.Windows;

namespace SSCM.Core;

public interface IPlatform
{
    public DateTime UtcNow { get; }
    public string WorkingDir { get; }
    public string ProgramFilesDir { get; }
    public string UserDocumentsDir { get; }

    public void Open(string path);
}

public class Platform : IPlatform
{
    public DateTime UtcNow => DateTime.UtcNow;
    public string WorkingDir => Directory.GetCurrentDirectory();
    public string ProgramFilesDir => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    public string UserDocumentsDir => Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
    
    public void Open(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }
        
        new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            }
        }.Start();
    }
}
