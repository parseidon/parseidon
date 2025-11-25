namespace Parseidon.Parser.Grammar.Terminals;

public class BooleanTerminal : AbstractValueTerminal
{
    public BooleanTerminal(Boolean value, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Value = value;
    }

    public Boolean Value { get; }

    public override String AsText() => Value.ToString().ToLower();

}
