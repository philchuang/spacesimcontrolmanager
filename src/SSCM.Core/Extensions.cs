using Microsoft.Extensions.Configuration;

namespace SSCM.Core;

public static class Extensions
{
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