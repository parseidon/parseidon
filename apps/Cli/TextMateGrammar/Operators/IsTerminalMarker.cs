using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class IsTerminalMarker : AbstractMarker
{
    public IsTerminalMarker(Boolean doNetEscape, AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node)
    {
        DoNotEscape = doNetEscape;
    }

    public Boolean DoNotEscape { get; }
    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"MakeTerminal(actualNode, state, errorName, {DoNotEscape.ToString().ToLower()}, \n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
