using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Terminals;

public abstract class AbstractValueTerminal : AbstractFinalTerminal
{
    public AbstractValueTerminal(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    public override bool MatchesVariableText() => false;

    public abstract String AsText();

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        var rules = new (String Search, String Replace)[]
        {
                (".", "\\."),
                ("^", "\\^"),
                ("$", "\\$"),
                ("*", "\\*"),
                ("+", "\\+"),
                ("?", "\\?"),
                ("{", "\\{"),
                ("}", "\\}"),
                ("|", "\\|"),
                ("(", "\\("),
                (")", "\\)"),
                ("\\", "\\\\"),
                ("[", "\\["),
                ("]", "\\]")
        };
        var tempText = AsText();
        tempText = tempText.ReplaceAll(rules);
        return new RegExResult(tempText, Array.Empty<String>());
    }
}
