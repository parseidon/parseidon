namespace Parseidon.Parser.Grammar.Operators;

public class OptionalOperator : AbstractOneChildOperator
{
    public OptionalOperator(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckRange(actualNode, state, 0, 1,\n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

}
