using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }
}
