namespace Parseidon.Parser.Grammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractGrammarElement? left, AbstractGrammarElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckAnd(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Left.ToString(grammar)},") + "\n";
        result += Indent($"(actualNode, errorName) => {Right.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => (Left is null ? base.MatchesVariableText() : Left.MatchesVariableText()) || (Right is null ? base.MatchesVariableText() : Right.MatchesVariableText());

}
