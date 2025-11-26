using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractGrammarElement? left, AbstractGrammarElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = $"({Left.ToString(grammar)})({Right.ToString(grammar)})";
        return result;
    }
}
