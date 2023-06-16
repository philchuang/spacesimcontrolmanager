using SSCM.Core;

namespace SSCM.cli;

public class CliUserInput : IUserInput
{
    public int MultipleChoice(string message, IList<string> choices)
    {
        if (!choices.Any()) throw new IndexOutOfRangeException(choices.Count.ToString());
        if (choices.Count > 9) throw new IndexOutOfRangeException(choices.Count.ToString());
        Console.WriteLine(message);
        for (var i = 1; i <= choices.Count; i++)
        {
            Console.WriteLine($"{i}: {choices[i-1]}");
        }

        Console.Write($"[1-{choices.Count}]? ");
        ConsoleKeyInfo answer;
        do {
            answer = Console.ReadKey(true);
            this.CheckCancelled(answer);
            if (answer.Modifiers != 0) continue;
            if (!int.TryParse(answer.KeyChar.ToString(), out var i)) continue;
            if (i < 1 || i > choices.Count) continue;
            Console.WriteLine(answer.KeyChar);
            Console.WriteLine();
            return i - 1;
        } while (true);
    }

    public bool YesNo(string message, bool defaultAnswer = true)
    {
        Console.WriteLine(message);
        if (defaultAnswer) Console.Write("[Y/n]? ");
        else Console.Write("[n/Y]? ");
        ConsoleKeyInfo answer;
        do {
            answer = Console.ReadKey(true);
            this.CheckCancelled(answer);
            if (answer.Modifiers != 0) continue;
            var key = answer.KeyChar.ToString().ToUpperInvariant();
            if (key != "Y" && key != "N") continue;
            Console.WriteLine(answer.KeyChar);
            Console.WriteLine();
            return key == "Y";
        } while (true);
    }

    protected void CheckCancelled(ConsoleKeyInfo answer)
    {
        if (answer.Modifiers == ConsoleModifiers.Control && answer.KeyChar.ToString().ToUpperInvariant() == "C")
        {
            throw new UserInputCancelledException();
        }
    }
}