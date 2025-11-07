namespace Parseidon.Parser.Grammar.Terminals;

public class RegExTerminal : AbstractFinalTerminal
{
    public RegExTerminal(String regEx, Int32 quantifier, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        Quantifier = quantifier;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }
    public Int32 Quantifier { get; }

    public override String ToString(Grammar grammar) => $"CheckRegEx(actualNode, state, \"{ToLiteral(RegEx, false).Trim()}\", {Quantifier})";

    public override bool MatchesVariableText() => true;

}
