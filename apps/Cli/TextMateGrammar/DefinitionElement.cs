using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public class DefinitionElement : AbstractDefinitionElement
{
    public DefinitionElement(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        if (element == null)
            throw GetException("No child element!");
        Element = element;
        Element.Parent = this;
    }

    public AbstractGrammarElement Element { get; }

    public override String ToString(Grammar grammar)
    {
        return Element.ToString(grammar);
    }
}
