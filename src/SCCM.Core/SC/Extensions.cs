using System.Text.RegularExpressions;

namespace SCCM.Core.SC;

public static class Extensions
{
    private static Regex INPUT_REGEX = new Regex(@"^([A-z]+)(\d+)_(.+)$");

    public static (string Type, int Instance, string bind)? GetInputTypeAndInstance(this Mapping self)
    {
        var match = INPUT_REGEX.Match(self.Input);
        if (!match.Success) return null;

        var type = match.Groups[1].Value switch {
            "kb" => "keyboard",
            "js" => "joystick",
            _ => throw new ArgumentOutOfRangeException(),
        };
        var instance = int.Parse(match.Groups[2].Value);
        var bind = match.Groups[3].Value;

        return (type, instance, bind);
    }
    
    public static InputDevice GetRelatedInput(this MappingData self, Mapping mapping)
    {
        string type;
        int instance;
        try
        {
            var input = mapping.GetInputTypeAndInstance();
            if (input == null) throw new SccmException();
            type = input.Value.Type;
            instance = input.Value.Instance;
        }
        catch
        {
            throw new SccmException($"Mapping input [{mapping.Input}] for [{mapping.ActionMap}-{mapping.Action}] is invalid.");
        }

        try
        {
            return self.Inputs.First(i => i.Type == type && i.Instance == instance);
        }
        catch
        {
            throw new SccmException($"Could not find [{type}] [{instance}].");
        }
    }

    public static IEnumerable<Mapping> GetRelatedMappings(this MappingData self, InputDevice input)
    {
        var prefix = ActionMapsXmlHelper.GetInputPrefixForInputDevice(input);
        return self.Mappings.Where(m => m.Input.StartsWith(prefix));
    }
}