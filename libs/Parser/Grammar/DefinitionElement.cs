namespace Parseidon.Parser.Grammar;

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

    public override bool MatchesVariableText() => Element!.MatchesVariableText();

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            Element.IterateElements(process);
    }

}
