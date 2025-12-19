namespace Parseidon.Parser.Grammar.Operators;

public class ZeroOrMoreOperator : AbstractOneChildOperator
{
    public ZeroOrMoreOperator(AbstractDefinitionElement? terminal, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(terminal, calcLocation, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckZeroOrMore(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var elementRegEx = Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
        return new RegExResult($"(?:{elementRegEx.RegEx})*", elementRegEx.Captures);
    }
}
