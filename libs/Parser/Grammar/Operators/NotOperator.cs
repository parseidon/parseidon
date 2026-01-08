using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Operators;

public class NotOperator : AbstractMarker
{
    public NotOperator(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckNot(actualNode, state, errorName,\n";
        result += $"(actualNode, errorName) => {Element?.ToParserCode(grammar)}".Indent() + "\n";
        result += ")";
        return result;
    }

    public override Boolean MatchesVariableText(Grammar grammar) => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var elementRegEx = Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
        return new RegExResult($"(?!{elementRegEx.RegEx}).", elementRegEx.Captures);
    }
}
