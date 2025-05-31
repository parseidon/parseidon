namespace Parseidon.Parser.Grammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractGrammarElement? left, AbstractGrammarElement? right) : base(left, right)
    {
    }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckAnd(actualNode, state, \n";
        result += Indent($"(actualNode) => {Left.ToString(grammar)},") + "\n";
        result += Indent($"(actualNode) => {Right.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => (Left is null ? base.MatchesVariableText() : Left.MatchesVariableText()) || (Right is null ? base.MatchesVariableText() : Right.MatchesVariableText());

}
