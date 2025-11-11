namespace Parseidon.Parser.Grammar.Operators;

public class DropMarker : AbstractInTreeMarker
{
    public DropMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"Drop(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
