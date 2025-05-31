namespace Parseidon.Parser.Grammar.Terminals;

public class TextTerminal : AbstractFinalTerminal
{
    public TextTerminal(String text)
    {
        Text = text;
    }

    public String Text { get; }

    public override String ToString(Grammar grammar) => $"CheckText(actualNode, state, \"{ToLiteral(Text, true)}\")";

    public override bool MatchesVariableText() => false;

}
