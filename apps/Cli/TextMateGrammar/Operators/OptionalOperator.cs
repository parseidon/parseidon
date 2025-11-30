using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class OptionalOperator : AbstractOneChildOperator
{
    public OptionalOperator(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }
}
