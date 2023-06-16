namespace SSCM.Core;

public class DefaultUserInput : IUserInput
{
    public bool YesNo(string message, bool defaultAnswer = true) => defaultAnswer;
    public int MultipleChoice(string message, IList<string> choices) => 0;
}