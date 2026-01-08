using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractDefinitionElement? left, AbstractDefinitionElement? right, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(left, right, calcLocation, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckAnd(actualNode, state, errorName,\n";
        result += $"(actualNode, errorName) => {Left.ToParserCode(grammar)},".Indent() + "\n";
        result += $"(actualNode, errorName) => {Right.ToParserCode(grammar)}".Indent() + "\n";
        result += ")";
        return result;
    }

    public override Boolean MatchesVariableText(Grammar grammar) => (Left is null ? base.MatchesVariableText(grammar) : Left.MatchesVariableText(grammar)) || (Right is null ? base.MatchesVariableText(grammar) : Right.MatchesVariableText(grammar));

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var leftRegEx = Left.GetRegEx(grammar);
        var rightRegEx = Right.GetRegEx(grammar);
        return new RegExResult($"{leftRegEx.RegEx}{rightRegEx.RegEx}", leftRegEx.Captures.Concat(rightRegEx.Captures).ToArray());
    }
}
