namespace Parseidon.Parser.Grammar.Operators;

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
        result += $"MakeTerminal(actualNode, state, errorName, {DoNotEscape.ToString().ToLower()}, \n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
