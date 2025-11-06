namespace Parseidon.Parser.Grammar.Operators;

public class ZeroOrMoreOperator : AbstractOneChildOperator
{
    public ZeroOrMoreOperator(AbstractGrammarElement? terminal, MessageContext messageContext, ASTNode node) : base(terminal, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckZeroOrMore(actualNode, state, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;
}
