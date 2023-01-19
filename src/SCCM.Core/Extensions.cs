namespace SCCM.Core;

public static class Extensions
{
    public static int IntTryParseOrDefault(string? s, int def)
    {
        if (s != null && int.TryParse(s, out var i)) { return i; }
        return def;
    }
}