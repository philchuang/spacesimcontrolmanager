using SSCM.Core;
using System.Text;

namespace SSCM.Elite;

public class MappingMergeResult : MappingMergeResultBase<MappingData>
{
    public MappingMergeResult(MappingData current, MappingData updated) : base(current, updated)
    {
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        return sb.ToString();
    }
}