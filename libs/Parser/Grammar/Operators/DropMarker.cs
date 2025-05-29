namespace Parseidon.Parser.Grammar.Operators;

public class DropMarker : AbstractMarker
{
    // public override String ToString(Grammar grammar) => "true";    
    public DropMarker(AbstractGrammarElement? element) : base(element)
    {
    }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"Drop(actualNode, state, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }    
}
