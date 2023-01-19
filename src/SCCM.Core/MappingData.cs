using System.Text.RegularExpressions;

namespace SCCM.Core;

public class MappingData
{
    public DateTime? ReadTime { get; set; } = null;
    public IList<InputDevice> Inputs { get; set; } = new List<InputDevice>();
    public IList<Mapping> Mappings { get; set; } = new List<Mapping>();

    public InputDevice GetRelatedInput(Mapping mapping)
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
            return this.Inputs.First(i => i.Type == type && i.Instance == instance);
        }
        catch
        {
            throw new SccmException($"Could not find [{type}] [{instance}].");
        }
    }

    public IEnumerable<Mapping> GetRelatedMappings(InputDevice input)
    {
        var prefix = input.GetMappingPrefix();
        return this.Mappings.Where(m => m.Input.StartsWith(prefix));
    }
}