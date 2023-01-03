namespace SCCM.Core;

public static class Extensions
{
    public static int IntTryParseOrDefault(string? s, int def)
    {
        if (s != null && int.TryParse(s, out var i)) { return i; }
        return def;
    }

    public static bool HasChangedInputInstanceId(this ComparisonResult<InputDevice> self)
    {
        return self.Changed.Any(HasChangedInputInstanceId);
    }

    public static bool HasChangedInputInstanceId(this ComparisonPair<InputDevice> self)
    {
        return self.Current.Instance != self.Updated.Instance;
    }
}