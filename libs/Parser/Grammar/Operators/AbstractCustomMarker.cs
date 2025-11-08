using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractCustomMarker : AbstractMarker
{
    public AbstractCustomMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override bool MatchesVariableText() => true;

    public override AbstractGrammarElement GetMarkedRule(AbstractGrammarElement definition, List<AbstractMarker> customMarker)
    {
        customMarker.Add(this);
        return definition;
    }

}
