using SCCM.Core;

namespace SCCM.Tests.Mocks;

public class PlatformForTest : IPlatform
{
    public DateTime UtcNow { get; private set; }
    public string ProgramFilesDir { get; private set; }
    public string UserDocumentsDir { get; private set; }

    public PlatformForTest(DateTime? utcnow = null, string? programFilesDir = null, string? userDocumentsDir = null)
    {
        this.UtcNow = utcnow ?? DateTime.UtcNow;
        this.ProgramFilesDir = programFilesDir ?? string.Empty;
        this.UserDocumentsDir = userDocumentsDir ?? string.Empty;
    }
}