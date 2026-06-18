using System.Xml.Linq;
using SSCM.Core;
using SSCM.StarCitizen;
using static SSCM.Core.XmlExtensions;

public interface ISCDefaultProfileLoader
{
    Task<IList<SCMapping>> Load();
}

public class SCDefaultProfileLoader : ISCDefaultProfileLoader
{
    private readonly IPlatform _platform;

    public SCDefaultProfileLoader(IPlatform platform)
    {
        this._platform = platform;
    }

    public async Task<IList<SCMapping>> Load()
    {
        var mappings = new List<SCMapping>();
        
        var path = Path.Combine(this._platform.WorkingDir, "defaultProfile.xml");
        XDocument xd;
        try
        {
            xd = await XmlExtensions.LoadAsync(path).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            // TODO show warning
            return mappings;
        }

        if (xd?.Root == null)
        {
            return mappings;
        }

        // Iterate over every actionmap node
        var actionmapNodes = xd.Root.GetChildren("actionmap");
        foreach (var actionmap in actionmapNodes)
        {
            var actionmapName = actionmap.GetAttribute("name");
            if (string.IsNullOrEmpty(actionmapName))
            {
                continue;
            }

            // Capture every child action node
            var actionNodes = actionmap.GetChildren("action");
            foreach (var action in actionNodes)
            {
                var actionName = action.GetAttribute("name");
                if (string.IsNullOrEmpty(actionName))
                {
                    continue;
                }

                // Create SCMapping object and set properties
                var mapping = new SCMapping
                {
                    ActionMap = actionmapName,
                    Action = actionName
                };

                mappings.Add(mapping);
            }
        }

        return mappings;
    }
}