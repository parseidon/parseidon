namespace Parseidon.Parser.Grammar.Terminals;

public class NumberTerminal : AbstractFinalTerminal
{
    public NumberTerminal(Int32 number, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Number = number;
    }

    public Int32 Number { get; }

    public override String ToString(Grammar grammar) => $"CheckText(actualNode, state, \"{ToLiteral(Number.ToString(), true)}\")";

    public override bool MatchesVariableText() => false;

}
