using Newtonsoft.Json;

namespace SCCM.Tests;

public static class Extensions
{
    public static T JsonCopy<T>(this T self)
    {
        if (self == null) throw new ArgumentNullException(nameof(self));
        var json = JsonConvert.SerializeObject(self);
        var clone = JsonConvert.DeserializeObject<T>(json);
        return clone;
    }

    public static string RandomString() => Guid.NewGuid().ToString();
}