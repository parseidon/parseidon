using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class OrOperator : AbstractTwoChildOperator
{
    public OrOperator(AbstractGrammarElement? left, AbstractGrammarElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
