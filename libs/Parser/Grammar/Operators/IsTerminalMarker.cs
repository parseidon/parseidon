using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Operators;

public class IsTerminalMarker : AbstractMarker
{
    public IsTerminalMarker(Boolean doNetEscape, AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node)
    {
        DoNotEscape = doNetEscape;
    }

    public Boolean DoNotEscape { get; }
    public override String ToParserCode(Grammar grammar)
    {
        String result = "";
        result += $"MakeTerminal(actualNode, state, errorName, {DoNotEscape.ToString().ToLower()}, \n";
        result += $"(actualNode, errorName) => {Element?.ToParserCode(grammar)}".Indent() + "\n";
        result += ")";
        return result;
    }
}
