using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class IsTerminalMarker : AbstractMarker
{
    public IsTerminalMarker(Boolean doNetEscape, AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node)
    {
        DoNotEscape = doNetEscape;
    }

    public Boolean DoNotEscape { get; }
    public override String ToString(Grammar grammar)
    {
        String result = "";
        return result;
    }
}
