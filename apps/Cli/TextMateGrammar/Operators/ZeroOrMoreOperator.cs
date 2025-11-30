using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class ZeroOrMoreOperator : AbstractOneChildOperator
{
    public ZeroOrMoreOperator(AbstractDefinitionElement? terminal, MessageContext messageContext, ASTNode node) : base(terminal, messageContext, node) { }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }
}
