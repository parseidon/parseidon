namespace Parseidon.Parser.Grammar.Operators;

public class OneOrMoreOperator : AbstractOneChildOperator
{
    public OneOrMoreOperator(AbstractGrammarElement? element) : base(element)
    {
    }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckOneOrMore(actualNode, state, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool IsStatic() => false;

}
