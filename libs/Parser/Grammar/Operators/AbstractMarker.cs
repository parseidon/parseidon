using Humanizer;
using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    protected Definition GetDefinition()
    {
        AbstractGrammarElement? current = Parent;
        while (current is not null)
        {
            if (current is Definition definition)
                return definition;
            current = current.Parent;
        }
        throw new InvalidCastException("Found nor definition for marker!");
    }

    public override bool MatchesVariableText() => true;

    public override String ToParserCode(Grammar grammar) => Element?.ToParserCode(grammar) ?? String.Empty;

}
