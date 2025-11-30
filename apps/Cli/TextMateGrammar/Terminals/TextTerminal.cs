using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class TextTerminal : AbstractValueTerminal
{
    public TextTerminal(String text, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Text = text;
    }

    public String Text { get; }

    public override String AsText() => Text;

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        String tempText = $"{before.GetBeginRegEx()}{Text}{after.GetEndRegEx()}";
        String[] lines = tempText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        if (lines.Count() == 1)
            return new RegExMatchResult(Text.Length > 1 ? $"({Text})" : Text, null, Text.Length > 1 ? 1 : 0);
        return new RegExBeginEndResult(lines.First(), lines.Last(), null, 0, null, 0, null);
    }

}
