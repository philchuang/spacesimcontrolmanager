using System.Text.RegularExpressions;

namespace SSCM.Elite;

public class EDMappingConfig
{
    public Dictionary<string, List<string>> GroupMappings { get; set; } = new Dictionary<string, List<string>>();
    public List<string> IgnoreList { get; set; } = new List<string>();

    private readonly Lazy<IList<Tuple<Regex, string>>> _lazyGroupRegexes;
    private IList<Tuple<Regex, string>> GroupRegexes => this._lazyGroupRegexes.Value;

    private readonly Lazy<IList<Regex>> _lazyIgnoreRegexes;
    private IList<Regex> IgnoreRegexes => this._lazyIgnoreRegexes.Value;

    public EDMappingConfig()
    {
        this._lazyGroupRegexes = new Lazy<IList<Tuple<Regex, string>>>(() => this.SetupGroupRegexes().ToList());
        this._lazyIgnoreRegexes = new Lazy<IList<Regex>>(() => this.SetupIgnoreRegexes().ToList());
    }

    private IEnumerable<Tuple<Regex, string>> SetupGroupRegexes()
    {
        foreach (var kvp in this.GroupMappings)
        {
            var groupName = kvp.Key;
            foreach (var regex in kvp.Value)
            {
                yield return new Tuple<Regex, string>(new Regex(regex), groupName);
            }
        }
    }

    private IEnumerable<Regex> SetupIgnoreRegexes()
    {
        foreach (var s in this.IgnoreList)
        {
            yield return new Regex(s);
        }
    }

    public IList<string> GetGroupsForMapping(string mapping)
    {
        var matches = this.GroupRegexes.Where(t => t.Item1.IsMatch(mapping)).Select(t => t.Item2).Distinct().ToList();
        return matches.Any() ? matches : new string[] { "TBD" };
    }

    public bool IsIgnored(string mapping)
    {
        return this.IgnoreRegexes.Any(r => r.IsMatch(mapping));
    }

    public static EDMappingConfig Load(string path)
    {
        // Debug();
        using (var fs = File.OpenRead(path))
        using (var sr = new StreamReader(fs))
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var config = deserializer.Deserialize<EDMappingConfig>(sr);
            return config;
        }
    }

    // public static void Debug()
    // {
    //     var path = Path.Combine(Directory.GetCurrentDirectory(), "EDMappingConfig.actual.yml");
    //     var config = new EDMappingConfig
    //     {
    //         GroupMappings = new Dictionary<string, List<string>> {
    //             ["Driving"] = new List<string> { "^.*Buggy.*$" },
    //             ["Camera"] = new List<string> { "^.*FreeCam.*$", "^Cam.+$" }
    //         },
    //         IgnoreList = { "KeyboardLayout" },
    //     };
        
    //     using (var sw = File.CreateText(path))
    //     {
    //         var serializer = new YamlDotNet.Serialization.Serializer();
    //         serializer.Serialize(sw, config);
    //     }
        
    //     using (var fs = File.OpenRead(path))
    //     using (var sr = new StreamReader(fs))
    //     {
    //         var deserializer = new YamlDotNet.Serialization.Deserializer();
    //         deserializer.Deserialize<EDMappingConfig>(sr);
    //     }
    // }
}

public class EDGroupMapping
{
    public string GroupName { get; set; } = string.Empty;

    public List<string> Regexes { get; set; } = new List<string>();
}
