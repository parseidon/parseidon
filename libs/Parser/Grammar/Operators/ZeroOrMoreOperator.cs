namespace Parseidon.Parser.Grammar.Operators;

public class ZeroOrMoreOperator : AbstractOneChildOperator
{
    public ZeroOrMoreOperator(AbstractDefinitionElement? terminal, MessageContext messageContext, ASTNode node) : base(terminal, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckZeroOrMore(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;
}
