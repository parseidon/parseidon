namespace Parseidon.Parser.Grammar.Operators;

public class IsTerminalMarker : AbstractMarker
{
    public IsTerminalMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"MakeTerminal(actualNode, state, \n";
        result += Indent($"(actualNode) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }
}
