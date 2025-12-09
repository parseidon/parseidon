using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class AndOperator : AbstractTwoChildOperator
{
    public AndOperator(AbstractDefinitionElement? left, AbstractDefinitionElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckAnd(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Left.ToParserCode(grammar)},") + "\n";
        result += Indent($"(actualNode, errorName) => {Right.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => (Left is null ? base.MatchesVariableText() : Left.MatchesVariableText()) || (Right is null ? base.MatchesVariableText() : Right.MatchesVariableText());

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var leftRegEx = Left.GetRegEx(grammar);
        var rightRegEx = Right.GetRegEx(grammar);
        return new RegExResult($"{leftRegEx.RegEx}{rightRegEx.RegEx}", leftRegEx.Captures.Concat(rightRegEx.Captures).ToArray());
    }
}
