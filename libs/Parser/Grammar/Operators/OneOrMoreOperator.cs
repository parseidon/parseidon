namespace Parseidon.Parser.Grammar.Operators;

public class OneOrMoreOperator : AbstractOneChildOperator
{
    public OneOrMoreOperator(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckOneOrMore(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

}
