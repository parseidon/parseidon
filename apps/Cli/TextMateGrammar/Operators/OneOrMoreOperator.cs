using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class OneOrMoreOperator : AbstractOneChildOperator
{
    public OneOrMoreOperator(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
