namespace SSCM.Core;

public class MappingData
{
    public DateTime? ReadTime { get; set; } = null;
    public IList<InputDevice> Inputs { get; set; } = new List<InputDevice>();
    public IList<Mapping> Mappings { get; set; } = new List<Mapping>();
}