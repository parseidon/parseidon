using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class NumberTerminal : AbstractValueTerminal
{
    public NumberTerminal(Int32 number, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Number = number;
    }

    public Int32 Number { get; }

    public override String AsText() => Number.ToString();

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        return new RegExMatchResult(Number.ToString().Length > 1 ? $"({Number.ToString()})" : Number.ToString(), null, Number.ToString().Length > 1 ? 1 : 0);
    }
}
