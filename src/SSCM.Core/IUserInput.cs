namespace SSCM.Core;

public interface IUserInput
{
    bool YesNo(string message, bool defaultAnswer = true);
    int MultipleChoice(string message, IList<string> choices);
}