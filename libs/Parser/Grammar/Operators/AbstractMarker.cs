namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractMarker : AbstractOneChildOperator
{
    public AbstractMarker(AbstractGrammarElement? element) : base(element)
    {
    }

    public override bool MatchesVariableText() => true;
}
