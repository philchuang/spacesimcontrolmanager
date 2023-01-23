using System.Text.RegularExpressions;

namespace SSCM.Elite;

public class EDMappingConfig
{
    public Dictionary<string, List<string>> GroupMappings { get; set; } = new Dictionary<string, List<string>>();
    public List<string> IgnoreList { get; set; } = new List<string>();

    private readonly Lazy<IList<Tuple<Regex, string>>> _lazyRegexes;
    private IList<Tuple<Regex, string>> Regexes => this._lazyRegexes.Value;

    public EDMappingConfig()
    {
        this._lazyRegexes = new Lazy<IList<Tuple<Regex, string>>>(() => this.SetupRegexes().ToList());
    }

    private IEnumerable<Tuple<Regex, string>> SetupRegexes()
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

    public string GetGroupForMapping(string mapping)
    {
        foreach (var tuple in Regexes)
        {
            if (tuple.Item1.IsMatch(mapping)) return tuple.Item2;
        }

        return "TBD";
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
