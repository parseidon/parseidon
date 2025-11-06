namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractOperator : AbstractDefinitionElement
{
    public AbstractOperator(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }
}
