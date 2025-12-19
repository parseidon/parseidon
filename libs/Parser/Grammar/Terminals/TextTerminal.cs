using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Terminals;

public class TextTerminal : AbstractValueTerminal
{
    public TextTerminal(String text, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Text = text;
    }

    public String Text { get; }

    public override String ToParserCode(Grammar grammar) => $"CheckText(actualNode, state, errorName, \"{ToLiteral(Text, true)}\")";

    public override String AsText() => Text.Unescape();
}
