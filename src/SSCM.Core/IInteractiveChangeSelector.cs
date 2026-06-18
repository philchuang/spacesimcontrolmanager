namespace SSCM.Core;

public interface IInteractiveChangeSelector
{
    bool SelectAndApply(InteractiveChangeSession session);
}
