using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class DropMarker : AbstractMarker
{
    public DropMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
