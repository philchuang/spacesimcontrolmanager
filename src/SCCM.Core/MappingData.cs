using System.Text.RegularExpressions;

namespace SCCM.Core;

public class MappingData
{
    public DateTime? ReadTime { get; set; } = null;
    public IList<InputDevice> Inputs { get; set; } = new List<InputDevice>();
    public IList<Mapping> Mappings { get; set; } = new List<Mapping>();

    public InputDevice GetInputForMapping(Mapping mapping)
    {
        string type = string.Empty;
        int instance = 0;
        try
        {
            var match = Regex.Match(mapping.Input, @"^([A-z]+)(\d+)_(.+)$");
            if (!match.Success) throw new FormatException();
            type = match.Groups[1].Value switch {
                "kb" => "keyboard",
                "js" => "joystick",
                _ => throw new ArgumentOutOfRangeException(),
            };
            instance = int.Parse(match.Groups[2].Value);
        }
        catch
        {
            throw new SccmException($"Mapping input [{mapping.Input}] for [{mapping.ActionMap}-{mapping.Action}] is invalid.");
        }

        try
        {
            return this.Inputs.First(i => i.Type == type && i.Instance == instance);
        }
        catch
        {
            throw new SccmException($"Could not find [{type}] [{instance}].");
        }
    }
}