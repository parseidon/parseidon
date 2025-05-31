using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public class OrOperator : AbstractTwoChildOperator
{
    public OrOperator(AbstractGrammarElement? left, AbstractGrammarElement? right) : base(left, right)
    {
    }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckOr(actualNode, state, \n";
        result += Indent($"(actualNode) => {Left.ToString(grammar)},") + "\n";
        result += Indent($"(actualNode) => {Right.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

}
