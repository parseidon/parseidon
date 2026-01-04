using Humanizer;
using Parseidon.Parser.Grammar.Blocks;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

    protected Definition GetDefinition()
    {
        AbstractGrammarElement? current = Parent;
        while (current is not null)
        {
            if (current is Definition definition)
                return definition;
            current = current.Parent;
        }
        throw new InvalidCastException("Found no definition for marker!");
    }

    public override Boolean MatchesVariableText(Grammar grammar) => Element?.MatchesVariableText(grammar) ?? false;

    public override String ToParserCode(Grammar grammar) => Element?.ToParserCode(grammar) ?? String.Empty;

}
