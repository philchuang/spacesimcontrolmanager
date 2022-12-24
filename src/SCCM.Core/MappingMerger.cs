namespace SCCM.Core;

public class MappingMerger
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private MappingMergeResult _result = new MappingMergeResult(new MappingData(), new MappingData(), new ComparisonResult<InputDevice>(), new ComparisonResult<Mapping> ());

    private void CalculateDiffs(MappingData current, MappingData updated)
    {
        // capture differences
        this._result = new MappingMergeResult(
            current,
            updated,
            ComparisonHelper.Compare(
                current.Inputs, updated.Inputs,
                i => $"{i.Type}-{i.Product}",
                (p, c) => p.Instance != c.Instance ||
                    ComparisonHelper.DictionariesAreDifferent(
                        p.Settings.ToDictionary(s => s.Name), 
                        c.Settings.ToDictionary(s => s.Name))),
            ComparisonHelper.Compare(
                current.Mappings, updated.Mappings,
                m => $"{m.ActionMap}-{m.Action}",
                (p, c) => p.Input != c.Input || p.MultiTap != c.MultiTap)
        );
        this.AnalyzeResult();
    }

    private void AnalyzeResult()
    {
        this._result.HasDifferences = this._result.InputDiffs.Any() || this._result.MappingDiffs.Any();
        this._result.CanMerge = true;
        this.AnalyzeInputDiffs();
        this.AnalyzeMappingDiffs();
    }

    private void StopMerge()
    {
        this._result.CanMerge = false;
        this._result.MergeActions.Clear();
    }

    private void AnalyzeInputDiffs()
    {
        if (!this._result.CanMerge) return;

        if (this._result.InputDiffs.HasChangedInputInstanceId())
        {
            // input device changed - if instance changed, can't merge because that would change all the bindings
            this.StopMerge();
            return;
        }

        foreach (var input in this._result.InputDiffs.Added)
        {
            // input device added - add to current
            this._result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Add, input));
        }

        foreach (var input in this._result.InputDiffs.Removed)
        {
            // input device removed - if referenced by preserved binding, can't merge - else remove current
            if (this._result.Current.GetRelatedMappings(input).Any(m => m.Preserve))
            {
                this.StopMerge();
                return;
            }

            this._result.MergeActions.Add(new MappingMergeAction(null, MappingMergeActionMode.Remove, input));
        }

        foreach (var pair in this._result.InputDiffs.Changed)
        {
            // check settings
            /* - setting added - add with preserve = true
             * - setting removed - remove if current preserve == false - else keep current
             * - setting changed - update if preserve == false - else keep current
             */
            // TODO implement
        }
    }

    private void AnalyzeMappingDiffs()
    {
        if (!this._result.CanMerge) return;
        
        /* what to do:
         * - binding added - add to current
         * - binding removed - remove if current preserve == false - else keep current
         * - binding changed - update if preserve == false - else keep current
         */

        foreach (var mapping in this._result.MappingDiffs.Added)
        {
            // TODO implement
        }

        foreach (var mapping in this._result.MappingDiffs.Removed)
        {
            // TODO implement
        }

        foreach (var pair in this._result.MappingDiffs.Changed)
        {
            // TODO implement
        }
    }

    public bool Preview(MappingData current, MappingData updated)
    {
        this.CalculateDiffs(current, updated);

        if (!this._result.HasDifferences) return false;

        this.StandardOutput(this._result.ToString());
        return true;
    }

    public MappingData Merge(MappingData current, MappingData updated)
    {
        this.CalculateDiffs(current, updated);
        
        return current;
    }
}