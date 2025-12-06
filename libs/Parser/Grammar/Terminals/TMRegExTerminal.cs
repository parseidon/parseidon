namespace Parseidon.Parser.Grammar.Terminals;

public class TMRegExTerminal : AbstractFinalTerminal
{
    public TMRegExTerminal(String regEx, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }
}
