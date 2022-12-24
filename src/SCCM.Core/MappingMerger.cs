namespace SCCM.Core;

public class MappingMerger
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private MappingMergeResult _result = new MappingMergeResult(new ComparisonResult<InputDevice>(), new ComparisonResult<Mapping> ());

    private void CalculateDiffs(MappingData current, MappingData updated)
    {
        // capture differences
        this._result = new MappingMergeResult(
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
        this.AnalyzeInputDiffs();
        this.AnalyzeMappingDiffs();
    }

    private void AnalyzeInputDiffs()
    {
        /* what to do
         * - input device added - add to current
         * - input device removed - if referenced by preserved binding, can't merge - else remove current
         * - input device changed - if instance changed, can't merge because that would change all the bindings
         *   - setting added - add with preserve = true
         *   - setting removed - remove if current preserve == false - else keep current
         *   - setting changed - update if preserve == false - else keep current
         */

        foreach (var inputId in this._result.InputDiffs.AddedKeys)
        {
            // TODO implement
        }

        foreach (var inputId in this._result.InputDiffs.RemovedKeys)
        {
            // TODO implement
        }

        foreach (var pair in this._result.InputDiffs.ChangedPairs)
        {
            if (pair.HasChangedInputInstanceId()) this._result.CanMerge = false;
            // TODO continue
        }
    }

    private void AnalyzeMappingDiffs()
    {
        /* what to do:
         * - binding added - add to current
         * - binding removed - remove if current preserve == false - else keep current
         * - binding changed - update if preserve == false - else keep current
         */

        foreach (var mappingId in this._result.MappingDiffs.AddedKeys)
        {
            // TODO implement
        }

        foreach (var mappingId in this._result.MappingDiffs.RemovedKeys)
        {
            // TODO implement
        }

        foreach (var pair in this._result.MappingDiffs.ChangedPairs)
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