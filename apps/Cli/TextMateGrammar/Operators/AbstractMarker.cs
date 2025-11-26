using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    protected SimpleRule GetRule()
    {
        AbstractGrammarElement? current = Parent;
        while (current is not null)
        {
            if (current is SimpleRule rule)
                return rule;
            current = current.Parent;
        }
        throw new InvalidCastException("Found nor rule for marker!");
    }

    public override String ToString(Grammar grammar) => Element?.ToString(grammar) ?? String.Empty;
}
