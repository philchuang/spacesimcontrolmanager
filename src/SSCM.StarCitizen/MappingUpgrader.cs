using System.ComponentModel;
using SSCM.Core;
using SSCM.StarCitizen.Bindings;

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

    public const string UpgradeMappingsFileName = "SCUpgradeMappings.jsonc";

    private readonly ISCFolders _folders;
    private readonly SCDefaultProfileFileSource _defaultProfile;
    private Dictionary<string, UpgradeMapping> _upgradeMappings = new Dictionary<string, UpgradeMapping>();
    private Dictionary<string, SCMapping> _defaultMappings = new Dictionary<string, SCMapping>();

    public MappingUpgrader(IPlatform platform, ISCFolders folders) : this(platform, folders, SCDefaultProfileFileSource.Create(platform))
    {
    }

    /// <param name="executableDir">The fallback location for SCUpgradeMappings.jsonc. Defaults to the executable directory.</param>
    public MappingUpgrader(IPlatform platform, ISCFolders folders, SCDefaultProfileFileSource defaultProfile, string? executableDir = null) : base(platform)
    {
        this._folders = folders;
        this._defaultProfile = defaultProfile;
        this.LoadMappings(ResourceFile.Create(platform, UpgradeMappingsFileName, executableDir));
        this.LoadDefaultMappings();
    }

    private void LoadMappings(ResourceFile file)
    {
        try
        {
            var mappings = UpgradeMapping.Load(file.Require()).Result;
            this._upgradeMappings = mappings.ToDictionary(m => $"{m.Type}-{m.Source}".ToLowerInvariant());
        }
        catch // (Exception ex)
        {
            base._StandardOutput($"ERROR: Failed to load SC upgrade mappings from {file.Searched}!");
        }
    }

    private void LoadDefaultMappings()
    {
        if (this._defaultProfile.FindExistingPath() == null)
        {
            base._StandardOutput($"ERROR: Failed to load SC default profile from {this._defaultProfile.Searched}!");
        }

        var mappings = this._defaultProfile.LoadMappings().Result;
        foreach (var mapping in mappings)
        {
            this._defaultMappings[$"mapping-{mapping.ActionMap}-{mapping.Action}".ToLowerInvariant()] = mapping;
        }
    }

    public override async Task<SCMappingData> Upgrade(SCMappingData current)
    {
        var updated = current.JsonCopy();

        foreach (var input in updated.Inputs)
        {
            foreach (var setting in input.Settings.ToList())
            {
                if (!this._upgradeMappings.TryGetValue($"setting-{setting.Name}".ToLowerInvariant(), out var map)) continue;

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
            if (this._upgradeMappings.TryGetValue($"mapping-{mapping.ActionMap}-{mapping.Action}".ToLowerInvariant(), out var upgradeMapping))
            {
                if (upgradeMapping.Target == null)
                {
                    base._StandardOutput($"REMOVING: {mapping.ActionMap}-{mapping.Action}...");
                    updated.Mappings.Remove(mapping);
                    this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping));
                    continue;
                }

                base._StandardOutput($"RENAMING: {mapping.ActionMap}-{mapping.Action} to {upgradeMapping.Target}...");
                var s = upgradeMapping.Target.Split("-");
                mapping.ActionMap = s[0];
                mapping.Action = s[1];
                this.ResultSC.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, mapping));
            }
            else if (!this._defaultMappings.TryGetValue($"mapping-{mapping.ActionMap}-{mapping.Action}".ToLowerInvariant(), out var defaultMapping))
            {
                base._StandardOutput($"WARNING: {mapping.ActionMap}-{mapping.Action} not found in default mappings...");
                continue;
            }
        }

        this.ResultSC.HasDifferences = this.ResultSC.MergeActions.Any();
        return updated;
    }
}