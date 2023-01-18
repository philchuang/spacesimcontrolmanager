using System.Text.RegularExpressions;

namespace SSCM.Core.StarCitizen;

public static class Extensions
{
    private static Regex INPUT_REGEX = new Regex(@"^([A-z]+)(\d+)_(.+)$");

    public static (string Type, int Instance, string bind)? GetInputTypeAndInstance(this Mapping self)
    {
        var match = INPUT_REGEX.Match(self.Input);
        if (!match.Success) return null;

        var type = ActionMapsXmlHelper.GetOptionsTypeFromAbbv(match.Groups[1].Value);
        var instance = int.Parse(match.Groups[2].Value);
        var bind = match.Groups[3].Value;

        return (type, instance, bind);
    }

    public static string GetInputPrefix(this InputDevice self)
    {
        var typeAbbv = ActionMapsXmlHelper.GetOptionsTypeAbbv(self.Type);
        var prefix = $"{typeAbbv}{self.Instance}_";
        return prefix;
    }
    
    public static InputDevice GetRelatedInput(this MappingData self, Mapping mapping)
    {
        string type;
        int instance;
        try
        {
            var input = mapping.GetInputTypeAndInstance();
            if (input == null) throw new SscmException();
            type = input.Value.Type;
            instance = input.Value.Instance;
        }
        catch
        {
            throw new SscmException($"Mapping input [{mapping.Input}] for [{mapping.ActionMap}-{mapping.Action}] is invalid.");
        }

        try
        {
            return self.Inputs.First(i => i.Type == type && i.Instance == instance);
        }
        catch
        {
            throw new SscmException($"Could not find [{type}] [{instance}].");
        }
    }

    public static IEnumerable<Mapping> GetRelatedMappings(this MappingData self, InputDevice input)
    {
        var prefix = input.GetInputPrefix();
        return self.Mappings.Where(m => m.Input.StartsWith(prefix));
    }
}