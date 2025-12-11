namespace Parseidon.Parser.Grammar.Operators;

public class OrOperator : AbstractTwoChildOperator
{
    public OrOperator(AbstractDefinitionElement? left, AbstractDefinitionElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"CheckOr(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Left.ToParserCode(grammar)},") + "\n";
        result += Indent($"(actualNode, errorName) => {Right.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var leftRegEx = Left.GetRegEx(grammar);
        var rightRegEx = Right.GetRegEx(grammar);
        return new RegExResult($"(?:(?:{leftRegEx.RegEx})|(?:{rightRegEx.RegEx}))", leftRegEx.Captures.Concat(rightRegEx.Captures).ToArray());
    }
}
