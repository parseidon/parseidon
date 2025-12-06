namespace Parseidon.Parser.Grammar.Terminals;

public class TextTerminal : AbstractValueTerminal
{
    public TextTerminal(String text, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Text = text;
    }

    public String Text { get; }

    public override String ToParserCode(Grammar grammar) => $"CheckText(actualNode, state, errorName, \"{ToLiteral(Text, true)}\")";

    public override String AsText() => Text;

}
