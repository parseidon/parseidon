using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class OneOrMoreOperator : AbstractOneChildOperator
{
    public OneOrMoreOperator(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }
}
