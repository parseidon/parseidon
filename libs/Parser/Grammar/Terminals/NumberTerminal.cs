namespace Parseidon.Parser.Grammar.Terminals;

public class NumberTerminal : AbstractFinalTerminal
{
    public NumberTerminal(Int32 number, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Number = number;
    }

    public Int32 Number { get; }

    public override bool MatchesVariableText() => false;

}
