namespace Parseidon.Parser.Grammar.Operators;

public class NotOperator : AbstractMarker
{
    public NotOperator(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckNot(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var elementRegEx = Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
        return new RegExResult($"(?!{elementRegEx.RegEx})", elementRegEx.Captures);
    }
}
