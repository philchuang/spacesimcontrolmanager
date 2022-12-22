namespace SCCM.Core;

public interface IPlatform
{
    public DateTime UtcNow { get; }
}

public class Platform : IPlatform
{
    public DateTime UtcNow { get { return DateTime.UtcNow; } }
}
