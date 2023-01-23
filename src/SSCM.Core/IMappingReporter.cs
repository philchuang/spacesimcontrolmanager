namespace SSCM.Core;

public interface IMappingReporter<TData>
{
    string Report(TData data, bool preservedOnly);
}
