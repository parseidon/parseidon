using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public class OrOperator : AbstractTwoChildOperator
{
    public OrOperator(AbstractGrammarElement? left, AbstractGrammarElement? right, MessageContext messageContext, ASTNode node) : base(left, right, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"CheckOr(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Left.ToString(grammar)},") + "\n";
        result += Indent($"(actualNode, errorName) => {Right.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => true;

}
