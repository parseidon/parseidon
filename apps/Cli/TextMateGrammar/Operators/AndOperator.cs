using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractDefinitionElement? left, AbstractDefinitionElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }
}
