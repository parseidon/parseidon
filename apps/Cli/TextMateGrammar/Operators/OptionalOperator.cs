using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class OptionalOperator : AbstractOneChildOperator
{
    public OptionalOperator(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
