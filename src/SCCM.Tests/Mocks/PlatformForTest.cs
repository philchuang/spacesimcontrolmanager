using SCCM.Core;

namespace SCCM.Tests.Mocks;

public class PlatformForTest : IPlatform
{
    public DateTime UtcNow { get; private set; }

    public PlatformForTest(DateTime utcnow)
    {
        this.UtcNow = utcnow;
    }
}