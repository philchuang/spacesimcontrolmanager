using SSCM.Core;

namespace SSCM.Elite;

public class MappingImportMerger : MappingImportMergerBase<EDMappingData>
{
    public MappingMergeResult ResultED {
        get;
        set;
    } = new MappingMergeResult(
        new EDMappingData(), 
        new EDMappingData(), 
        new ComparisonResult<EDMapping>(), 
        new ComparisonResult<EDMappingSetting>());

    public override MappingMergeResultBase<EDMappingData> Result {
        get => this.ResultED;
        set => this.ResultED = (MappingMergeResult) value;
    }

    protected override InteractiveChangeRow CreateInteractiveRow(EDMappingData current, MappingMergeAction action)
    {
        if (action.Value is EDMapping mapping)
        {
            var currentMapping = current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
            var currentValue = currentMapping?.PreservedBindings ?? "";
            return this.CreateInteractiveRow(action, mapping.Id, currentValue, mapping.PreservedBindings, () => this.ApplyMappingAction(current, action, mapping));
        }
        if (action.Value is ValueTuple<EDMapping, string> tuple)
        {
            var updatedMapping = tuple.Item1;
            var bindingType = tuple.Item2;
            var currentMapping = current.Mappings.Single(m => m.Name == updatedMapping.Name);
            return this.CreateInteractiveRow(action, $"{updatedMapping.Id}.{bindingType}", currentMapping.GetBinding(bindingType)?.ToString() ?? "", updatedMapping.GetBinding(bindingType)?.ToString() ?? "", () => this.ApplyMappingBindingAction(current, action, updatedMapping, bindingType));
        }
        if (action.Value is EDMappingSetting setting)
        {
            var currentSetting = this.GetSettingList(current, setting).SingleOrDefault(s => s.Name == setting.Name);
            var currentValue = currentSetting?.Value ?? "";
            return this.CreateInteractiveRow(action, setting.Id, currentValue, setting.Value, () => this.ApplySettingAction(current, action, setting));
        }

        throw UnsupportedActionValue(action);
    }

    private bool ApplyMappingAction(EDMappingData current, MappingMergeAction action, EDMapping mapping)
    {
        return ApplyListAction(current.Mappings, action, mapping, m => m.Name == mapping.Name);
    }

    private bool ApplyMappingBindingAction(EDMappingData current, MappingMergeAction action, EDMapping updatedMapping, string bindingType)
    {
        var currentMapping = current.Mappings.Single(m => m.Name == updatedMapping.Name);
        CreateBindingSetter(bindingType)(currentMapping, updatedMapping.GetBinding(bindingType));
        return true;
    }

    private bool ApplySettingAction(EDMappingData current, MappingMergeAction action, EDMappingSetting setting)
    {
        var list = this.GetSettingList(current, setting);
        return ApplyListAction(list, action, setting, s => s.Name == setting.Name);
    }

    private IList<EDMappingSetting> GetSettingList(EDMappingData data, EDMappingSetting setting)
    {
        var split = setting.Group.Split(".");
        return split.Length switch {
            2 => data.Mappings.Single(m => m.Name == split[1]).Settings,
            1 => data.Settings,
            _ => throw new FormatException($"Unable to parse setting group [{setting.Group}]"),
        };
    }

    protected override EDMappingData Merge(EDMappingData current, EDMappingData updated, IUserInput userInput)
    {
        this.CalculateDiffs(current, updated);

        // TODO-WIP rethink whether or not CanAutoMerge is meaningful anymore - partial merges, interactive merges, etc.
        // if (!this.Result.CanAutoMerge) return current;

        if (!userInput.YesNo("\nStart interactive merge?"))
        {
            throw new UserInputCancelledException();
        }

        for (var i = 0; i < this.Result.MergeActions.Count; i++)
        {
            var action = this.Result.MergeActions[i];
            if (action.Value is EDMapping mapping)
            { // whole mapping added/removed
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    if (userInput.YesNo($"Add MAPPING [{mapping.Id}] += {mapping.PreservedBindings} ?"))
                        current.Mappings.Add(mapping);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} MAPPING [{mapping.Id}] -= {mapping.PreservedBindings} ?", !action.ExistingIsPreserved))
                    {
                        var toRemove = current.Mappings.Single(m => m.Name == mapping.Name);
                        current.Mappings.Remove(toRemove);
                    }
                }
            }
            else if (action.Value is ValueTuple<EDMapping, string> tuple)
            { // mapping binding changed
                var updatedMapping = tuple.Item1;
                var currentMapping = current.Mappings.Single(m => m.Name == updatedMapping.Name);
                var bindingType = tuple.Item2;
                var bindingGetter = CreateBindingGetter(bindingType);
                var bindingSetter = CreateBindingSetter(bindingType);

                if (userInput.YesNo($"Update{(action.ExistingIsPreserved ? " PRESERVED" : "")} MAPPING [{currentMapping.Id}-{bindingType}] {bindingGetter(currentMapping)} => {bindingGetter(updatedMapping)} ?", !action.ExistingIsPreserved))
                {
                    bindingSetter(currentMapping, bindingGetter(updatedMapping));
                }
            }
            else if (action.Value is EDMappingSetting setting)
            { // mapping setting or setting added/removed/changed
                var list = this.GetSettingList(current, setting);
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    if (userInput.YesNo($"Add SETTING [{setting.Name}] += {setting.Value} ?"))
                        list.Add(setting);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} SETTING [{setting.Name}] -= {setting.Value} ?", !action.ExistingIsPreserved))
                    {
                        var toRemove = list.Single(s => s.Name == setting.Name);
                        list.Remove(toRemove);
                    }
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    if (userInput.YesNo($"Update{(action.ExistingIsPreserved ? " PRESERVED" : "")} SETTING [{setting.Name}] => {setting.Value} ?", !action.ExistingIsPreserved))
                    {
                        var toRemove = list.Single(s => s.Name == setting.Name);
                        var idx = list.IndexOf(toRemove);
                        list.Insert(idx, setting);
                        list.RemoveAt(idx + 1);
                    }
                }
            }
        }

        return current;
    }

    private static Func<EDMapping, EDBinding?> CreateBindingGetter(string bindingType)
    {
        return bindingType switch {
            nameof(EDMapping.Binding) => e => e.Binding,
            nameof(EDMapping.Primary) => e => e.Primary,
            nameof(EDMapping.Secondary) => e => e.Secondary,
            _ => throw new ArgumentOutOfRangeException(bindingType)
        };
    }

    private static Action<EDMapping, EDBinding?> CreateBindingSetter(string bindingType)
    {
        return bindingType switch {
            nameof(EDMapping.Binding) => (e, b) => e.Binding = b,
            nameof(EDMapping.Primary) => (e, b) => e.Primary = b,
            nameof(EDMapping.Secondary) => (e, b) => e.Secondary = b,
            _ => throw new ArgumentOutOfRangeException(bindingType)
        };
    }

    protected override void CalculateDiffs(EDMappingData current, EDMappingData updated)
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
        this.Result.CanAutoMerge = true;
        this.AnalyzeMappingDiffs();
        this.AnalyzeSettingDiffs();
        this.Result.CanAutoMerge = this.Result.CanAutoMerge && this.Result.MergeActions.Any();
    }

    private void StopMerge()
    {
        this.Result.CanAutoMerge = false;
        this.Result.MergeActions.Clear();
    }

    private void AnalyzeMappingDiffs()
    {
        var addedBindingHelper = (string mappingId, EDBinding? binding, string type) =>
        {
            if (binding == null) return;
            this.WriteLineStandard($"MAPPING added and will auto-merge: [{mappingId}-{type}] => {binding}");
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
                this.WriteLineStandard($"MAPPING removed and will auto-merge: [{mapping.Id}] -= {mapping.PreservedBindings}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping));
            }
            else
            {
                this.WriteLineStandard($"MAPPING removed and will not auto-merge: [{mapping.Id}] => {mapping.PreservedBindings}");
                this.Result.CanAutoMerge = false;
            }
        }

        var changedBindingHelper = (EDMapping mapping, EDBinding? current, EDBinding? updated, string type) =>
        {
            if (current == null || updated == null) return;
            if (EquateBindings(current, updated)) return; // wasn't this binding
            if (!current.Preserve)
            {
                this.WriteLineStandard($"MAPPING changed and will auto-merge: [{mapping.Id}-{type}] {current} => {updated}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, (mapping, type)));
            }
            else
            {
                this.WriteLineStandard($"MAPPING changed and will not auto-merge: [{mapping.Id}-{type}] => {current} != {updated}");
                this.Result.CanAutoMerge = false;
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
            this.WriteLineStandard($"SETTING added and will auto-merge: [{setting.Id}] => {setting.Value}");
            setting.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, setting));
        }

        foreach (var setting in this.ResultED.SettingDiffs.Removed)
        {
            // setting removed - remove if current preserve == false - else keep current
            if (!setting.Preserve)
            {
                this.WriteLineStandard($"SETTING removed and will auto-merge: [{setting.Id}] -= {setting.Value}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, setting));
            }
            else
            {
                this.WriteLineStandard($"SETTING removed and will not auto-merge: [{setting.Id}] => {setting.Value}");
                this.Result.CanAutoMerge = false;
            }
        }

        foreach (var pair in this.ResultED.SettingDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.WriteLineStandard($"SETTING changed and will auto-merge: [{pair.Current.Id}] {pair.Current.Value} => {pair.Updated.Value}");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated));
            }
            else
            {
                this.WriteLineStandard($"SETTING changed and will not auto-merge: [{pair.Current.Id}] => {pair.Current.Value} != {pair.Updated.Value}");
                this.Result.CanAutoMerge = false;
            }
        }
    }
}
