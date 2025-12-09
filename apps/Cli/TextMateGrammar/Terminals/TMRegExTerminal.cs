using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class TMRegExTerminal : AbstractDefinitionElement
{
    public TMRegExTerminal(String regEx, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult(RegEx, Array.Empty<String>());
    }
}
