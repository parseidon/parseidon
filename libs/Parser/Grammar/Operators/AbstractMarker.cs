using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override bool MatchesVariableText() => true;

}
