namespace Parseidon.Parser.Grammar.Operators;

public class DropMarker : AbstractInTreeMarker
{
    // public override String ToString(Grammar grammar) => "true";    
    public DropMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"Drop(actualNode, state, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
