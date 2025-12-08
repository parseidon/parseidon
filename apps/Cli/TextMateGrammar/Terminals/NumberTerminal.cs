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

}
