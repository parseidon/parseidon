namespace Parseidon.Parser.Grammar.Terminals;

public class RegExTerminal : AbstractFinalTerminal
{
    public RegExTerminal(String regEx)
    {
        RegEx = regEx;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }

    public override String ToString(Grammar grammar) => $"CheckRegEx(actualNode, state, \"{ToLiteral(RegEx, false).Trim()}\")"; 

    public override bool IsStatic() => false;

}
