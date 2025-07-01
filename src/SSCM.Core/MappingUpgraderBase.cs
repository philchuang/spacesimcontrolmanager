using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Core;

public abstract class MappingUpgraderBase<TData> : IMappingUpgrader<TData>
{
    public event Action<string> StandardOutput = delegate { };
    protected void _StandardOutput(string s) => this.StandardOutput(s);
    public event Action<string> WarningOutput = delegate { };
    protected void _WarningOutput(string s) => this.WarningOutput(s);
    public event Action<string> DebugOutput = delegate { };
    protected void _DebugOutput(string s) => this.DebugOutput(s);

    public abstract MappingMergeResultBase<TData> Result { get; set; }

    protected readonly IPlatform _platform;

    protected MappingUpgraderBase(IPlatform platform)
    {
        this._platform = platform;
    }

    public virtual async Task<bool> Preview(TData current)
    {
        var upgrade = await this.Upgrade(current);
        return this.Result.HasDifferences;
    }

    public abstract Task<TData> Upgrade(TData current);
}