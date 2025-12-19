namespace Parseidon.Parser.Grammar.Terminals;

public class NumberTerminal : AbstractValueTerminal
{
    public NumberTerminal(Int32 number, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Number = number;
    }

    public Int32 Number { get; }

    public override String AsText() => Number.ToString();

}
