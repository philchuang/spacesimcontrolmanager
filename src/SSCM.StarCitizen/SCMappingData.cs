namespace SSCM.StarCitizen;

public class SCMappingData
{
    public DateTime? ReadTime { get; set; } = null;
    public IList<SCInputDevice> Inputs { get; set; } = new List<SCInputDevice>();
    public IList<SCMapping> Mappings { get; set; } = new List<SCMapping>();
}