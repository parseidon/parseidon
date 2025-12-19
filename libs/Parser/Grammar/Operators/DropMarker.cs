namespace Parseidon.Parser.Grammar.Operators;

public class DropMarker : AbstractMarker
{
    public DropMarker(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"Drop(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
