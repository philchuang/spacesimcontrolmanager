using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SSCM.Core;

public static class Extensions
{
    public static T JsonCopy<T>(this T self)
    {
        if (self == null) throw new ArgumentNullException(nameof(self));
        var json = JsonConvert.SerializeObject(self);
        var clone = JsonConvert.DeserializeObject<T>(json);
        return clone!;
    }

    public static string? GetValueOrNull(this IConfiguration self, string sectionName, string settingName)
    {
        var section = self.GetSection(sectionName);
        return section[settingName].ValueOrNull();
    }

    private static string? ValueOrNull(this string? self) => string.IsNullOrWhiteSpace(self) ? null : self;

    public static bool AddIfNotNull<T>(this IList<T> self, T? item)
        where T: class
    {
        if (item == null) return false;
        
        self.Add(item);
        return true;
    }
}