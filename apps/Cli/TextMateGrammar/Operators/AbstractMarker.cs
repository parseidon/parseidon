using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }
}
