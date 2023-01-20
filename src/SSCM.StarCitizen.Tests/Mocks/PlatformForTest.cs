using SSCM.Core;

namespace SSCM.Tests.Mocks;

public class PlatformForTest : IPlatform
{
    public DateTime UtcNow { get; private set; }
    public string ProgramFilesDir { get; private set; }
    public string UserDocumentsDir { get; private set; }

    public Action<string> OpenMock { get; private set; }

    public PlatformForTest(DateTime? utcnow = null, string? programFilesDir = null, string? userDocumentsDir = null, Action<string>? openMock = null)
    {
        this.UtcNow = utcnow ?? DateTime.UtcNow;
        this.ProgramFilesDir = programFilesDir ?? string.Empty;
        this.UserDocumentsDir = userDocumentsDir ?? string.Empty;
        this.OpenMock = openMock ?? (s => {});
    }

    public void Open(string path)
    {
        this.OpenMock(path);
    }
}