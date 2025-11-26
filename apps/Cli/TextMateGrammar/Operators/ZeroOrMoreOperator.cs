using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class ZeroOrMoreOperator : AbstractOneChildOperator
{
    public ZeroOrMoreOperator(AbstractDefinitionElement? terminal, MessageContext messageContext, ASTNode node) : base(terminal, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
