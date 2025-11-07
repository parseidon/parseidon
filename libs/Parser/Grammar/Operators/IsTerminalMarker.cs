namespace Parseidon.Parser.Grammar.Operators;

public class IsTerminalMarker : AbstractMarker
{
    public IsTerminalMarker(Boolean doNetEscape, AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node)
    {
        DoNotEscape = doNetEscape;
    }

    public Boolean DoNotEscape { get; }
    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"MakeTerminal(actualNode, state, {DoNotEscape.ToString().ToLower()}, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
