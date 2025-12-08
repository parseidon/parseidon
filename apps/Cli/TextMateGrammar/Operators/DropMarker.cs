using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class DropMarker : AbstractMarker
{
    public DropMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"Drop(actualNode, state, errorName,\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
