using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class RegExTerminal : AbstractFinalTerminal
{
    public RegExTerminal(String regEx, Int32 quantifier, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        RegEx = regEx;
        Quantifier = quantifier;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }
    public Int32 Quantifier { get; }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        return new RegExMatchResult(RegEx, null, 0);
    }
}
