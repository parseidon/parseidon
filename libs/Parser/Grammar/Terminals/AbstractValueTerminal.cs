namespace Parseidon.Parser.Grammar.Terminals;

public abstract class AbstractValueTerminal : AbstractFinalTerminal
{
    public AbstractValueTerminal(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    public override bool MatchesVariableText() => false;

    public abstract String AsText();

}
