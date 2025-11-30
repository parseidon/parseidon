using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class BooleanTerminal : AbstractValueTerminal
{
    public BooleanTerminal(Boolean value, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Value = value;
    }

    public Boolean Value { get; }

    public override String AsText() => Value.ToString().ToLower();

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        return new RegExMatchResult($"({Value.ToString().ToLower()})", null, 1);
    }
}
