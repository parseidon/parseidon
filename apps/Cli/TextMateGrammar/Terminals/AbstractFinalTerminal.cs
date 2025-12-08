using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public abstract class AbstractFinalTerminal : AbstractDefinitionElement
{
    public AbstractFinalTerminal(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

}
