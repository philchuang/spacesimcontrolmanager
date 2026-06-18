using SSCM.Core;

public class UserInputForTest : IUserInput
{
    public Dictionary<string, string> Answers { get; } = new Dictionary<string, string>();

    protected TextWriter Out { get; init; }

    public bool Strict { get; set; }

    public UserInputForTest(TextWriter output)
    {
        this.Out = output;
    }

    public int MultipleChoice(string message, IList<string> choices)
    {
        return int.Parse(this.Answers[message]);
    }

    public bool YesNo(string message, bool defaultAnswer = true)
    {
        Out.WriteLine($"UserInputMessage: {message}");
        
        if (this.Answers.TryGetValue(message.Trim(), out var answer))
        {
            if (string.IsNullOrEmpty(answer)) return defaultAnswer;
            
            return answer.ToUpperInvariant() == "Y";
        }
        else if (this.Strict)
        {
            throw new KeyNotFoundException($"No answer found for message [{message.Trim()}]!");
        }

        return defaultAnswer;
    }
}