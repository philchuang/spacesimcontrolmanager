using SSCM.Core;
using System.Text;

namespace SSCM.Elite;

public class MappingMergeResult : MappingMergeResultBase<EDMappingData>
{
    public MappingMergeResult(EDMappingData current, EDMappingData updated) : base(current, updated)
    {
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        return sb.ToString();
    }
}