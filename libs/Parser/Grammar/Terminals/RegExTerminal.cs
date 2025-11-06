namespace Parseidon.Parser.Grammar.Terminals;

public class RegExTerminal : AbstractFinalTerminal
{
    public RegExTerminal(String regEx, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }

    public override String ToString(Grammar grammar) => $"CheckRegEx(actualNode, state, \"{ToLiteral(RegEx, false).Trim()}\")";

    public override bool MatchesVariableText() => true;

}
