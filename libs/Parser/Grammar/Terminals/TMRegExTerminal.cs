namespace Parseidon.Parser.Grammar.Terminals;

public class TMRegExTerminal : AbstractDefinitionElement
{
    public TMRegExTerminal(String regEx, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        if (RegEx.Length == 0)
            throw GetException("A regular expression can not be empty!");
    }

    public String RegEx { get; }

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult(RegEx, Array.Empty<String>());
    }
}
