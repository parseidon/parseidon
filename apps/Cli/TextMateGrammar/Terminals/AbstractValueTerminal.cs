using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public abstract class AbstractValueTerminal : AbstractFinalTerminal
{
    public AbstractValueTerminal(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    public abstract String AsText();
}
