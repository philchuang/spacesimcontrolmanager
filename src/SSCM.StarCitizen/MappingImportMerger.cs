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

        return this.Result.HasDifferences && this.Result.CanAutoMerge;
    }

    public SCMappingData Merge(SCMappingData current, SCMappingData updated)
    {
        return this.Merge(current, updated, new DefaultUserInput());
    }

    public SCMappingData MergeInteractive(SCMappingData current, SCMappingData updated, IUserInput userInput)
    {
        return this.Merge(current, updated, userInput);
    }

    protected SCMappingData Merge(SCMappingData current, SCMappingData updated, IUserInput userInput)
    {
        this.CalculateDiffs(current, updated);

        // TODO add input options: prev/next, finish, cancel. If revisiting a processed mapping, display the decided action.

        // TODO-WIP rethink whether or not CanAutoMerge is meaningful anymore - partial merges, interactive merges, etc.
        // if (!this.Result.CanAutoMerge) return current;

        if (!userInput.YesNo("\nStart interactive merge?"))
        {
            throw new UserInputCancelledException();
        }

        for (var i = 0; i < this.Result.MergeActions.Count; i++)
        {
            var action = this.Result.MergeActions[i];
            if (action.Value is SCInputDevice input)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    if (userInput.YesNo($"Add INPUT [{input.Id}]?"))
                        current.Inputs.Add(input);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} INPUT [{input.Id}]?", !action.ExistingIsPreserved))
                    {
                        current.Inputs.Remove(input);
                        // add remove actions for related mappings
                        current.Mappings.Where(m => m.Input == input.Id).Select(m => new MappingMergeAction(MappingMergeActionMode.Remove, m, false)).ToList().ForEach(this.Result.MergeActions.Add);
                    }
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    throw new InvalidOperationException($"Invalid combination of MappingMergeActionMode.Replace and InputDevice.");
                }
            }
            else if (action.Value is SCInputDeviceSetting setting)
            {
                var target = current.Inputs.Where(i => $"{i.Type}-{i.Instance}-{i.Product}" == setting.Parent).Single();
                var parentProduct = setting.Parent.Split("-")[2];
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    if (userInput.YesNo($"Add INPUT SETTING {parentProduct} [{setting.Name}] += {setting.Properties.EntriesToString()} ?"))
                       target.Settings.Add(setting);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?", !action.ExistingIsPreserved))
                        target.Settings.Remove(setting);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    if (userInput.YesNo($"Update{(action.ExistingIsPreserved ? " PRESERVED" : "")} INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?", !action.ExistingIsPreserved))
                    {
                        var currentSetting = target.Settings.Single(s => s.Name == setting.Name);
                        var idx = target.Settings.IndexOf(currentSetting);
                        target.Settings.Insert(idx, setting);
                        target.Settings.RemoveAt(idx + 1);
                    }
                }
            }
            else if (action.Value is SCMapping mapping)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    // TODO make this an option
                    // if (mapping.Input.EndsWith("_ ")) continue;

                    if (userInput.YesNo($"Add MAPPING [{mapping.Id}] += {mapping.InputToString} ?"))
                        current.Mappings.Add(mapping);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} MAPPING [{mapping.Id}] -= {mapping.InputToString} ?", !action.ExistingIsPreserved))
                        current.Mappings.Remove(mapping);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    // TODO is there a way to display current value?
                    if (userInput.YesNo($"Update{(action.ExistingIsPreserved ? " PRESERVED" : "")} MAPPING [{mapping.Id}] => {mapping.InputToString} ?", !action.ExistingIsPreserved))
                    {
                        var currentMapping = current.Mappings.Single(m => m.ActionMap == mapping.ActionMap && m.Action == mapping.Action);
                        var idx = current.Mappings.IndexOf(currentMapping);
                        current.Mappings.Insert(idx, mapping);
                        current.Mappings.RemoveAt(idx + 1);
                    }
                }
            }
            else if (action.Value is SCAttribute attribute)
            {
                if (action.Mode == MappingMergeActionMode.Add)
                {
                    if (userInput.YesNo($"Add ATTRIBUTE [{attribute.Name}] += {attribute.Value} ?"))
                        current.Attributes.Add(attribute);
                }
                else if (action.Mode == MappingMergeActionMode.Remove)
                {
                    if (userInput.YesNo($"Remove{(action.ExistingIsPreserved ? " PRESERVED" : "")} ATTRIBUTE [{attribute.Name}] -= {attribute.Value} ?", !action.ExistingIsPreserved))
                        current.Attributes.Remove(attribute);
                }
                else if (action.Mode == MappingMergeActionMode.Replace)
                {
                    if (userInput.YesNo($"Update{(action.ExistingIsPreserved ? " PRESERVED" : "")} ATTRIBUTE [{attribute.Name}] => {attribute.Value} ?", !action.ExistingIsPreserved))
                    {
                        var currentAttribute = current.Attributes.Single(a => a.Name == attribute.Name);
                        var idx = current.Attributes.IndexOf(currentAttribute);
                        current.Attributes.Insert(idx, attribute);
                        current.Attributes.RemoveAt(idx + 1);
                    }
                }
            }
        }
        
        if (!userInput.YesNo("\nFinish interactive merge and save changes?"))
        {
            throw new UserInputCancelledException();
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
        this.Result.CanAutoMerge = true;
        this.AnalyzeInputDiffs();
        this.AnalyzeMappingDiffs();
        this.AnalyzeAttributeDiffs();
        this.Result.CanAutoMerge &= this.Result.MergeActions.Any() && this.Result.MergeActions.All(m => !m.ExistingIsPreserved);
    }

    private void StopMerge()
    {
        this.Result.CanAutoMerge = false;
        this.Result.MergeActions.Clear();
    }

    private void AnalyzeInputDiffs()
    {
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
            this.StandardOutput($"INPUT added and will auto-merge: [{input.Id}]");
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, input));
        }

        foreach (var input in this.ResultSC.InputDiffs.Removed)
        {
            // input device removed - if referenced by preserved binding, can't merge - else remove current
            if (this.Result.Current.GetRelatedMappings(input).Any(m => m.Preserve))
            {
                this.StandardOutput($"INPUT removed but will not auto-merge: [{input.Id}] has preserved mappings.");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, input, true));
                this.Result.CanAutoMerge = false;
            }
            else
            {
                this.StandardOutput($"INPUT removed and will auto-merge: [{input.Id}]");
                this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, input));

                // remove all related mappings (they probably won't be in the updated MappingData anyway)
                var relatedMappings = this.Result.Updated.GetRelatedMappings(input).ToList();
                relatedMappings.ForEach(m => this.Result.Updated.Mappings.Remove(m));
            }
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

    private void AnalyzeInputSettingsDiffs(SCInputDevice input, ComparisonResult<SCInputDeviceSetting> settingsDiffs)
    {
        foreach (var setting in settingsDiffs.Added)
        {
            // setting added - add with preserve = true
            this.StandardOutput($"INPUT SETTING added and will auto-merge: {input.Product} [{setting.Name}] += {setting.Properties.EntriesToString()}");
            setting.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, setting));
        }

        foreach (var setting in settingsDiffs.Removed)
        {
            // setting removed - auto-remove if current preserve == false - else keep current
            if (!setting.Preserve)
            {
                this.StandardOutput($"INPUT SETTING removed and will auto-merge: {input.Product} [{setting.Name}] -= {setting.Properties.EntriesToString()}");
            }
            else
            {
                this.StandardOutput($"INPUT SETTING removed and will not auto-merge: {input.Product} [{setting.Name}] => {setting.Properties.EntriesToString()}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, setting, setting.Preserve));
        }

        foreach (var pair in settingsDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"INPUT SETTING changed and will auto-merge: {input.Product} [{pair.Current.Name}] {pair.Current.Properties.EntriesToString()} => {pair.Updated.Properties.EntriesToString()}");
            }
            else
            {
                this.StandardOutput($"INPUT SETTING changed and will not auto-merge: {input.Product} [{pair.Current.Name}] => {pair.Current.Properties.EntriesToString()} != {pair.Updated.Properties.EntriesToString()}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated, pair.Current.Preserve));
        }
    }

    private void AnalyzeMappingDiffs()
    {
        foreach (var mapping in this.ResultSC.MappingDiffs.Added)
        {
            // mapping added - add with preserve = true
            this.StandardOutput($"MAPPING added and will auto-merge: [{mapping.Id}] += {mapping.InputToString}");
            mapping.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, mapping));
        }

        foreach (var mapping in this.ResultSC.MappingDiffs.Removed)
        {
            // mapping removed - remove if current preserve == false - else keep current
            if (!mapping.Preserve)
            {
                this.StandardOutput($"MAPPING removed and will auto-merge: [{mapping.Id}] -= {mapping.InputToString}");
            }
            else
            {
                this.StandardOutput($"MAPPING removed and will not auto-merge: [{mapping.Id}] -= {mapping.InputToString}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, mapping, mapping.Preserve));
        }

        foreach (var pair in this.ResultSC.MappingDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"MAPPING changed and will auto-merge: [{pair.Current.Id}] {pair.Current.InputToString} => {pair.Updated.InputToString}");
            }
            else
            {
                this.StandardOutput($"MAPPING changed and will not auto-merge: [{pair.Current.Id}] => {pair.Current.InputToString} != {pair.Updated.InputToString}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated, pair.Current.Preserve));
        }
    }

    private void AnalyzeAttributeDiffs()
    {
        foreach (var attr in this.ResultSC.AttributeDiffs.Added)
        {
            // attribute added - add with preserve = true
            this.StandardOutput($"ATTRIBUTE added and will auto-merge: [{attr.Name}] += {attr.Value}");
            attr.Preserve = true;
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, attr));
        }

        foreach (var attr in this.ResultSC.AttributeDiffs.Removed)
        {
            // attribute removed - remove if current preserve == false - else keep current
            if (!attr.Preserve)
            {
                this.StandardOutput($"ATTRIBUTE removed and will auto-merge: [{attr.Name}] -= {attr.Value}");
            }
            else
            {
                this.StandardOutput($"ATTRIBUTE removed and will not auto-merge: [{attr.Name}] -= {attr.Value}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Remove, attr, attr.Preserve));
        }

        foreach (var pair in this.ResultSC.AttributeDiffs.Changed)
        {
            // setting changed - update if preserve == false - else keep current
            if (!pair.Current.Preserve)
            {
                this.StandardOutput($"ATTRIBUTE changed and will auto-merge: [{pair.Current.Name}] {pair.Current.Value} => {pair.Updated.Value}");
            }
            else
            {
                this.StandardOutput($"ATTRIBUTE changed and will not auto-merge: [{pair.Current.Name}] => {pair.Current.Value} != {pair.Updated.Value}");
                this.Result.CanAutoMerge = false;
            }
            this.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Replace, pair.Updated, pair.Current.Preserve));
        }
    }
}