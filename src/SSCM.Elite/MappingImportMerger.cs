using SSCM.Core;

namespace SSCM.Elite;

public class MappingImportMerger : IMappingImportMerger<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public MappingMergeResult ResultED {
        get;
        set;
    } = new MappingMergeResult(new EDMappingData(), new EDMappingData(), new ComparisonResult<EDMapping>(), new ComparisonResult<EDMappingSetting>());

    public MappingMergeResultBase<EDMappingData> Result {
        get => this.ResultED;
        set => this.ResultED = (MappingMergeResult) value;
    }

    public MappingImportMerger()
    {
    }

    public bool Preview(EDMappingData current, EDMappingData updated)
    {
        this.CalculateDiffs(current, updated);

        return this.Result.HasDifferences && this.Result.CanMerge;
    }

    public EDMappingData Merge(EDMappingData current, EDMappingData updated)
    {
        throw new NotImplementedException();
    }

    private void CalculateDiffs(EDMappingData current, EDMappingData updated)
    {
        // capture differences
        this.Result = new MappingMergeResult(
            current,
            updated,
            ComparisonHelper.Compare(
                current.Mappings, updated.Mappings,
                m => m.Name,
                EquateMappings),
            ComparisonHelper.Compare(
                current.Settings.Concat(current.Mappings.SelectMany(m => m.Settings)).ToList(), 
                updated.Settings.Concat(updated.Mappings.SelectMany(m => m.Settings)).ToList(),
                s => s.Id,
                EquateSettings)
        );
        this.AnalyzeResult();
    }

    private bool EquateMappings(EDMapping? current, EDMapping? updated)
    {
        if (current == null && updated == null) return true;
        if (current == null || updated == null) return false;

        return
            string.Equals(current.Name, updated.Name, StringComparison.OrdinalIgnoreCase) &&
            EquateBindings(current.Binding, updated.Binding) &&
            EquateBindings(current.Primary, updated.Primary) &&
            EquateBindings(current.Secondary, updated.Secondary);
            // testing mapping settings with global settings
    }

    private bool EquateBindings(EDBinding? current, EDBinding? updated)
    {
        if (current == null && updated == null) return true;
        if (current == null || updated == null) return false;

        return current.ToString() == updated.ToString();
    }

    private bool EquateSettings(EDMappingSetting? current, EDMappingSetting? updated)
    {
        if (current == null && updated == null) return true;
        if (current == null || updated == null) return false;

        return
            string.Equals(current.Name, updated.Name, StringComparison.OrdinalIgnoreCase) &&
            current.Value == updated.Value;
    }

    private void AnalyzeResult()
    {
        this.Result.HasDifferences = this.ResultED.MappingDiffs.Any() || this.ResultED.SettingDiffs.Any();
        this.Result.CanMerge = true;
        this.AnalyzeMappingDiffs();
        this.AnalyzeSettingDiffs();
        this.Result.CanMerge = this.Result.CanMerge && this.Result.MergeActions.Any();
    }

    private void StopMerge()
    {
        this.Result.CanMerge = false;
        this.Result.MergeActions.Clear();
    }

    private void AnalyzeMappingDiffs()
    {
        if (!this.Result.CanMerge) return;

        var addedBindingHelper = (string mappingId, EDBinding? binding, string type) =>
        {
            if (binding == null) return;
            this.StandardOutput($"MAPPING added and will merge: [{mappingId}-{type} => {binding}");
            binding.Preserve = true;
        };

        foreach (var mapping in this.ResultED.MappingDiffs.Added)
        {
            // mapping added - add with preserve = true
            addedBindingHelper(mapping.Id, mapping.Binding, nameof(mapping.Binding));
            addedBindingHelper(mapping.Id, mapping.Primary, nameof(mapping.Primary));
            addedBindingHelper(mapping.Id, mapping.Secondary, nameof(mapping.Secondary));
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, mapping));
        }

        foreach (var mapping in this.ResultED.MappingDiffs.Removed)
        {
            // mapping removed - remove if current preserve == false - else keep current
            if (!mapping.AnyPreserve)
            {
                this.StandardOutput($"MAPPING removed and will merge: [{mapping.Id}]");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping));
            }
            else
            {
                this.StandardOutput($"MAPPING removed and will not merge: [{mapping.Id}], preserving.");
            }
        }

        var changedBindingHelper = (EDMapping mapping, EDBinding? current, EDBinding? updated, string type) =>
        {
            if (current == null || updated == null) return;
            if (!current.Preserve)
            {
                this.StandardOutput($"MAPPING changed and will merge: [{mapping.Id}-{type}] {current} => {updated}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, (mapping, type)));
            }
            else
            {
                this.StandardOutput($"MAPPING changed and will not merge: [{mapping.Id}-{type}] {current} != {updated}");
            }
        };

        foreach (var pair in this.ResultED.MappingDiffs.Changed)
        {
            // part of a mapping changed - update if preserve == false - else keep current
            changedBindingHelper(pair.Updated, pair.Current.Binding, pair.Updated.Binding, nameof(pair.Current.Binding));
            changedBindingHelper(pair.Updated, pair.Current.Primary, pair.Updated.Primary, nameof(pair.Current.Primary));
            changedBindingHelper(pair.Updated, pair.Current.Secondary, pair.Updated.Secondary, nameof(pair.Current.Secondary));
        }
    }

    private void AnalyzeSettingDiffs()
    {

        foreach (var setting in this.ResultED.SettingDiffs.Added)
        {
            // setting added - add with preserve = true
            this.StandardOutput($"SETTING added and will merge: [{setting.Id}] => {setting.Value}");
            setting.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, setting));
        }

        foreach (var setting in this.ResultED.SettingDiffs.Removed)
        {
            // setting removed - remove if current preserve == false - else keep current
            if (!setting.Preserve)
            {
                this.StandardOutput($"SETTING removed and will merge: [{setting.Id}]");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, setting));
            }
            else
            {
                this.StandardOutput($"SETTING removed and will not merge: [{setting.Id}], preserving {setting.Value}");
            }
        }

        foreach (var pair in this.ResultED.SettingDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"SETTING changed and will merge: [{pair.Current.Id}] {pair.Current.Value} => {pair.Updated.Value}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.StandardOutput($"SETTING changed and will not merge: [{pair.Current.Id}] {pair.Current.Value} != {pair.Updated.Value}");
            }
        }
    }
}