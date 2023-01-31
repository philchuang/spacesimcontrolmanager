namespace SSCM.Elite;

public class EDMappingData
{
    public DateTime? ReadTime { get; set; } = null;
    public IList<EDMapping> Mappings { get; set; } = new List<EDMapping>();
    public IList<EDMappingSetting> Settings { get; set; } = new List<EDMappingSetting>();
}