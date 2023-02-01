using SSCM.Core;

namespace SSCM.StarCitizen;

public class MappingImportMerger : IMappingImportMerger<SCMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public MappingMergeResult ResultSC {
        get;
        set;
    } = new MappingMergeResult(
        new SCMappingData(), 
        new SCMappingData(), 
        new ComparisonResult<SCInputDevice>(), 
        new ComparisonResult<SCMapping>(),
        new ComparisonResult<SCAttribute>());

    public MappingMergeResultBase<SCMappingData> Result {
        get => this.ResultSC;
        set => this.ResultSC = (MappingMergeResult) value;
    }

    public MappingImportMerger()
    {
    }

    public bool Preview(SCMappingData current, SCMappingData updated)
    {
        this.CalculateDiffs(current, updated);

        return this.Result.HasDifferences && this.Result.CanMerge;
    }

    public SCMappingData Merge(SCMappingData current, SCMappingData updated)
    {
        this.CalculateDiffs(current, updated);

        if (!this.Result.CanMerge) return current;

        foreach (var action in this.Result.MergeActions)
        {
            if (action.Value is SCInputDevice input)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    current.Inputs.Add(input);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    current.Inputs.Remove(input);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    throw new InvalidOperationException($"Invalid combination of MappingMergeActionMode.Replace and InputDevice.");
                }
            }
            else if (action.Value is SCInputDeviceSetting setting)
            {
                var target = current.Inputs.Where(i => $"{i.Type}-{i.Instance}-{i.Product}" == setting.Parent).Single();
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    target.Settings.Add(setting);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    target.Settings.Remove(setting);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    var currentSetting = target.Settings.Single(s => s.Name == setting.Name);
                    var idx = target.Settings.IndexOf(currentSetting);
                    target.Settings.Insert(idx, setting);
                    target.Settings.RemoveAt(idx + 1);
                }
            }
            else if (action.Value is SCMapping mapping)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    current.Mappings.Add(mapping);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    current.Mappings.Remove(mapping);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    var currentMapping = current.Mappings.Single(m => m.ActionMap == mapping.ActionMap && m.Action == mapping.Action);
                    var idx = current.Mappings.IndexOf(currentMapping);
                    current.Mappings.Insert(idx, mapping);
                    current.Mappings.RemoveAt(idx + 1);
                }
            }
            else if (action.Value is SCAttribute attribute)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    current.Attributes.Add(attribute);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    current.Attributes.Remove(attribute);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    var currentAttribute = current.Attributes.Single(a => a.Name == attribute.Name);
                    var idx = current.Attributes.IndexOf(currentAttribute);
                    current.Attributes.Insert(idx, attribute);
                    current.Attributes.RemoveAt(idx + 1);
                }
            }
        }
        
        return current;
    }

    private void CalculateDiffs(SCMappingData current, SCMappingData updated)
    {
        // capture differences
        this.Result = new MappingMergeResult(
            current,
            updated,
            ComparisonHelper.Compare(
                current.Inputs, updated.Inputs,
                i => $"{i.Type}-{i.Product}",
                (c, u) => c.Instance == u.Instance &&
                    ComparisonHelper.DictionariesAreEqual(
                        c.Settings.ToDictionary(s => s.Name), 
                        u.Settings.ToDictionary(s => s.Name),
                        (cs, us) => ComparisonHelper.DictionariesAreEqual(cs.Properties, us.Properties))),
            ComparisonHelper.Compare(
                current.Mappings, updated.Mappings,
                m => $"{m.Id}-{m.InputType}",
                (c, u) => c.Input == u.Input && c.MultiTap == u.MultiTap),
            ComparisonHelper.Compare(
                current.Attributes, updated.Attributes,
                a => a.Name,
                (c, u) => c.Value == u.Value)
        );
        this.AnalyzeResult();
    }

    private void AnalyzeResult()
    {
        this.Result.HasDifferences = this.ResultSC.InputDiffs.Any() || this.ResultSC.MappingDiffs.Any() || this.ResultSC.AttributeDiffs.Any();
        this.Result.CanMerge = true;
        this.AnalyzeInputDiffs();
        this.AnalyzeMappingDiffs();
        this.AnalyzeAttributeDiffs();
        this.Result.CanMerge &= this.Result.MergeActions.Any();
    }

    private void StopMerge()
    {
        this.Result.CanMerge = false;
        this.Result.MergeActions.Clear();
    }

    private void AnalyzeInputDiffs()
    {
        if (!this.Result.CanMerge) return;

        if (this.ResultSC.InputDiffs.HasChangedInputInstanceId())
        {
            // input device changed - if instance changed, can't merge because that would change all the bindings
            this.StandardOutput("WARNING: Input Instance IDs have changed and prevents a merge. Please manually resolve or execute import overwrite.");
            this.StopMerge();
            return;
        }

        foreach (var input in this.ResultSC.InputDiffs.Added)
        {
            // input device added - add to current
            this.StandardOutput($"INPUT added and will merge: [{input.Id}]");
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, input));
        }

        foreach (var input in this.ResultSC.InputDiffs.Removed)
        {
            // input device removed - if referenced by preserved binding, can't merge - else remove current
            if (this.Result.Current.GetRelatedMappings(input).Any(m => m.Preserve))
            {
                this.StandardOutput($"INPUT removed but will prevent merge: [{input.Id}] has preserved mappings.");
                this.StopMerge();
                return;
            }

            this.StandardOutput($"INPUT removed and will merge: [{input.Id}]");
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, input));

            // remove all related mappings (they probably won't be in the updated MappingData anyway)
            var relatedMappings = this.Result.Updated.GetRelatedMappings(input).ToList();
            relatedMappings.ForEach(m => this.Result.Updated.Mappings.Remove(m));
        }

        // at this point, only settings have changed
        foreach (var pair in this.ResultSC.InputDiffs.Changed)
        {
            var settingDiffs = ComparisonHelper.Compare(
                pair.Current.Settings, 
                pair.Updated.Settings, 
                s => s.Name,
                (c, u) => ComparisonHelper.DictionariesAreEqual(c.Properties, u.Properties));

            this.AnalyzeInputSettingsDiffs(pair.Current, settingDiffs);
        }
    }

    private static string DictionaryToString(IDictionary<string, string> d)
    {
        return $"{{{string.Join(",", d.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"))}}}";
    }

    private void AnalyzeInputSettingsDiffs(SCInputDevice input, ComparisonResult<SCInputDeviceSetting> settingsDiffs)
    {
        foreach (var setting in settingsDiffs.Added)
        {
            // setting added - add with preserve = true
            this.StandardOutput($"INPUT SETTING added and will merge: {input.Product} [{setting.Name}] => {DictionaryToString(setting.Properties)}");
            setting.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, setting));
        }

        foreach (var setting in settingsDiffs.Removed)
        {
            // setting removed - remove if current preserve == false - else keep current
            if (!setting.Preserve)
            {
                this.StandardOutput($"INPUT SETTING removed and will merge: {input.Product} [{setting.Name}] -= {DictionaryToString(setting.Properties)}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, setting));
            }
            else
            {
                this.StandardOutput($"INPUT SETTING removed and will not merge: {input.Product} [{setting.Name}] => {DictionaryToString(setting.Properties)}");
            }
        }

        foreach (var pair in settingsDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"INPUT SETTING changed and will merge: {input.Product} [{pair.Current.Name}] {DictionaryToString(pair.Current.Properties)} => {DictionaryToString(pair.Updated.Properties)}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"INPUT SETTING changed and will not merge: {input.Product} [{pair.Current.Name}] => {DictionaryToString(pair.Current.Properties)} != {DictionaryToString(pair.Updated.Properties)}");
            }
        }
    }

    private void AnalyzeMappingDiffs()
    {
        if (!this.Result.CanMerge) return;

        foreach (var mapping in this.ResultSC.MappingDiffs.Added)
        {
            // mapping added - add with preserve = true
            this.StandardOutput($"MAPPING added and will merge: [{mapping.Id}] => {mapping.InputToString}");
            mapping.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, mapping));
        }

        foreach (var mapping in this.ResultSC.MappingDiffs.Removed)
        {
            // mapping removed - remove if current preserve == false - else keep current
            if (!mapping.Preserve)
            {
                this.StandardOutput($"MAPPING removed and will merge: [{mapping.Id}] -= {mapping.InputToString}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping));
            }
            else
            {
                this.StandardOutput($"MAPPING removed and will not merge: [{mapping.Id}] => {mapping.InputToString}");
            }
        }

        foreach (var pair in this.ResultSC.MappingDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"MAPPING changed and will merge: [{pair.Current.Id}] {pair.Current.InputToString} => {pair.Updated.InputToString}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"MAPPING changed and will not merge: [{pair.Current.Id}] => {pair.Current.InputToString} != {pair.Updated.InputToString}");
            }
        }
    }

    private void AnalyzeAttributeDiffs()
    {
        if (!this.Result.CanMerge) return;

        foreach (var attr in this.ResultSC.AttributeDiffs.Added)
        {
            // attribute added - add with preserve = true
            this.StandardOutput($"ATTRIBUTE added and will merge: [{attr.Name}] => {attr.Value}");
            attr.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, attr));
        }

        foreach (var attr in this.ResultSC.AttributeDiffs.Removed)
        {
            // attribute removed - remove if current preserve == false - else keep current
            if (!attr.Preserve)
            {
                this.StandardOutput($"ATTRIBUTE removed and will merge: [{attr.Name}] -= {attr.Value}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, attr));
            }
            else
            {
                this.StandardOutput($"ATTRIBUTE removed and will not merge: [{attr.Name}] => {attr.Value}");
            }
        }

        foreach (var pair in this.ResultSC.AttributeDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"ATTRIBUTE changed and will merge: [{pair.Current.Name}] {pair.Current.Value} => {pair.Updated.Value}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"ATTRIBUTE changed and will not merge: [{pair.Current.Name}] => {pair.Current.Value} != {pair.Updated.Value}");
            }
        }
    }
}
