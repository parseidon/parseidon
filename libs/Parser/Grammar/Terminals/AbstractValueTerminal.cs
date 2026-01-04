using Parseidon.Helper;

namespace Parseidon.Parser.Grammar.Terminals;

public abstract class AbstractValueTerminal : AbstractDefinitionElement
{
    public AbstractValueTerminal(Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node) { }

    public override Boolean MatchesVariableText(Grammar grammar) => false;

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
