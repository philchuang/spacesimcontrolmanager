using System.ComponentModel;
using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingUpgrader : MappingUpgraderBase<SCMappingData>
{
    public MappingMergeResult ResultSC
    {
        get;
        set;
    } = new MappingMergeResult(
        new SCMappingData(),
        new SCMappingData(),
        new ComparisonResult<SCInputDevice>(),
        new ComparisonResult<SCMapping>(),
        new ComparisonResult<SCAttribute>());

    public override MappingMergeResultBase<SCMappingData> Result
    {
        get => this.ResultSC;
        set => this.ResultSC = (MappingMergeResult) value;
    }

    private readonly ISCFolders _folders;

    private Dictionary<string, UpgradeMapping> _mappings = new Dictionary<string, UpgradeMapping>();

    public MappingUpgrader(IPlatform platform, ISCFolders folders) : base(platform)
    {
        this._folders = folders;
        this.LoadMappings();
    }

    private void LoadMappings()
    {
        var path = Path.Combine(this._platform.WorkingDir, "SCUpgradeMappings.jsonc");
        try
        {
            var mappings = UpgradeMapping.Load(path).Result;
            this._mappings = mappings.ToDictionary(m => $"{m.Type}-{m.Source}".ToLowerInvariant());
        }
        catch // (Exception ex)
        {
            base._StandardOutput($"ERROR: Failed to load SC upgrade mappings from [{path}]!");
        }
    }

    public override async Task<SCMappingData> Upgrade(SCMappingData current)
    {
        var updated = current.JsonCopy();

        foreach (var input in updated.Inputs)
        {
            foreach (var setting in input.Settings.ToList())
            {
                if (!this._mappings.TryGetValue($"setting-{setting.Name}".ToLowerInvariant(), out var map)) continue;

                if (map.Target == null)
                {
                    base._StandardOutput($"REMOVING: {setting.Name}...");
                    input.Settings.Remove(setting);
                    this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, setting));
                    continue;
                }

                base._StandardOutput($"RENAMING: {setting.Name} to {map.Target}...");
                setting.Name = map.Target;
                this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, setting));
            }
        }

        foreach (var mapping in updated.Mappings.ToList())
        {
            if (!this._mappings.TryGetValue($"mapping-{mapping.ActionMap}-{mapping.Action}".ToLowerInvariant(), out var map)) continue;

            if (map.Target == null)
            {
                base._StandardOutput($"REMOVING: {mapping.ActionMap}-{mapping.Action}...");
                updated.Mappings.Remove(mapping);
                this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping));
                continue;
            }

            base._StandardOutput($"RENAMING: {mapping.ActionMap}-{mapping.Action} to {map.Target}...");
            var s = map.Target.Split("-");
            mapping.ActionMap = s[0];
            mapping.Action = s[1];
            this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, mapping));
        }

        this.ResultSC.HasDifferences = this.ResultSC.MergeActions.Any();
        return updated;
    }
}