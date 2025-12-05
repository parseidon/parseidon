using Humanizer;
using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    protected Definition GetRule()
    {
        AbstractGrammarElement? current = Parent;
        while (current is not null)
        {
            if (current is Definition rule)
                return rule;
            current = current.Parent;
        }
        throw new InvalidCastException("Found nor rule for marker!");
    }

    public override bool MatchesVariableText() => true;

    public override String ToString(Grammar grammar) => Element?.ToString(grammar) ?? String.Empty;

}
