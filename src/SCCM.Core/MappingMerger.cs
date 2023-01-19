namespace SCCM.Core;

public class MappingMerger
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public int ChangesCount { get => this.Result.MergeActions.Count; }

    public MappingMergeResult Result { get; private set; } = new MappingMergeResult(new MappingData(), new MappingData(), new ComparisonResult<InputDevice>(), new ComparisonResult<Mapping> ());

    public MappingMerger()
    {
    }

    public bool Preview(MappingData current, MappingData updated)
    {
        this.CalculateDiffs(current, updated);

        if (!this.Result.HasDifferences) return false;

        return true;
    }

    public MappingData Merge(MappingData current, MappingData updated)
    {
        this.CalculateDiffs(current, updated);
        
        return current;
    }

    private void CalculateDiffs(MappingData current, MappingData updated)
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
                m => $"{m.ActionMap}-{m.Action}",
                (c, u) => c.Input == u.Input && c.MultiTap == u.MultiTap)
        );
        this.AnalyzeResult();
    }

    private void AnalyzeResult()
    {
        this.Result.HasDifferences = this.Result.InputDiffs.Any() || this.Result.MappingDiffs.Any();
        this.Result.CanMerge = true;
        this.AnalyzeInputDiffs();
        this.AnalyzeMappingDiffs();
        this.Result.CanMerge = this.Result.CanMerge && this.Result.MergeActions.Any();
    }

    private void StopMerge()
    {
        this.Result.CanMerge = false;
        this.Result.MergeActions.Clear();
    }

    private void AnalyzeInputDiffs()
    {
        if (!this.Result.CanMerge) return;

        if (this.Result.InputDiffs.HasChangedInputInstanceId())
        {
            // input device changed - if instance changed, can't merge because that would change all the bindings
            this.StandardOutput("WARNING: Input Instance IDs have changed and prevents a merge. Please manually resolve or execute import overwrite.");
            this.StopMerge();
            return;
        }

        foreach (var input in this.Result.InputDiffs.Added)
        {
            // input device added - add to current
            this.StandardOutput($"INPUT added and will merge: [{input.Product}]");
            this.Result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Add, input));
        }

        foreach (var input in this.Result.InputDiffs.Removed)
        {
            // input device removed - if referenced by preserved binding, can't merge - else remove current
            if (this.Result.Current.GetRelatedMappings(input).Any(m => m.Preserve))
            {
                this.StandardOutput($"INPUT removed but will prevent merge: [{input.Product}] has preserved mappings.");
                this.StopMerge();
                return;
            }

            this.StandardOutput($"INPUT removed and will merge: [{input.Product}]");
            this.Result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Remove, input));
        }

        // at this point, only settings have changed
        foreach (var pair in this.Result.InputDiffs.Changed)
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

    private void AnalyzeInputSettingsDiffs(InputDevice input, ComparisonResult<InputDeviceSetting> settingsDiffs)
    {
        foreach (var setting in settingsDiffs.Added)
        {
            // setting added - add with preserve = true
            this.StandardOutput($"INPUT SETTING added and will merge: [{input.Product}] [{setting.Name}] = {DictionaryToString(setting.Properties)}");
            setting.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(input, MappingMergeActionMode.Add, setting));
        }

        foreach (var setting in settingsDiffs.Removed)
        {
            // setting removed - remove if current preserve == false - else keep current
            if (!setting.Preserve)
            {
                this.StandardOutput($"INPUT SETTING removed and will merge: [{input.Product}] [{setting.Name}]");
                this.Result.MergeActions.Add(new MappingMergeAction(input, MappingMergeActionMode.Remove, setting));
            }
            else
            {
                this.StandardOutput($"INPUT SETTING removed and will not merge: [{input.Product}] [{setting.Name}] preserved");
            }
        }

        foreach (var pair in settingsDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"INPUT SETTING changed and will merge: [{input.Product}] [{pair.Current.Name}] = {DictionaryToString(pair.Updated.Properties)}");
                this.Result.MergeActions.Add(new MappingMergeAction(pair.Current, MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"INPUT SETTING changed and will not merge: [{input.Product}] [{pair.Current.Name}] preserved");
            }
        }
    }

    private void AnalyzeMappingDiffs()
    {
        if (!this.Result.CanMerge) return;

        foreach (var mapping in this.Result.MappingDiffs.Added)
        {
            // mapping added - add with preserve = true
            this.StandardOutput($"MAPPING added and will merge: [{mapping.ActionMap}-{mapping.Action}] = {mapping.Input}");
            mapping.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Add, mapping));
        }

        foreach (var mapping in this.Result.MappingDiffs.Removed)
        {
            // mapping removed - remove if current preserve == false - else keep current
            if (!mapping.Preserve)
            {
                this.StandardOutput($"MAPPING removed and will merge: [{mapping.ActionMap}-{mapping.Action}]");
                this.Result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Remove, mapping));
            }
            else
            {
                this.StandardOutput($"MAPPING removed and will not merge: [{mapping.ActionMap}-{mapping.Action}] preserved");
            }
        }

        foreach (var pair in this.Result.MappingDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"MAPPING changed and will merge: [{pair.Current.ActionMap}-{pair.Current.Action}] = {pair.Updated.Input}");
                this.Result.MergeActions.Add(new MappingMergeAction(pair.Current, MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"MAPPING changed and will not merge: [{pair.Current.ActionMap}-{pair.Current.Action}] preserved");
            }
        }
    }
}