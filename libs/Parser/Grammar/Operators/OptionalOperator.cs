namespace Parseidon.Parser.Grammar.Operators;

public class OptionalOperator : AbstractOneChildOperator
{
    public OptionalOperator(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckRange(actualNode, state, errorName, 0, 1,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var elementRegEx = Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
        return new RegExResult($"(?:{elementRegEx.RegEx})?", elementRegEx.Captures);
    }
}
