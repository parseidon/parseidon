namespace Parseidon.Parser.Grammar.Terminals;

public class BooleanTerminal : AbstractValueTerminal
{
    public BooleanTerminal(Boolean value, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Value = value;
    }

    public Boolean Value { get; }

    public override String AsText() => Value.ToString().ToLower();

}
