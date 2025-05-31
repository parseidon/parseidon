using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar;

public class DefinitionElement : AbstractDefinitionElement
{
    public DefinitionElement(AbstractGrammarElement? element)
    {
        if (element == null)
            throw new Exception("No child element!");
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
        if(process(this))
            Element.IterateElements(process);
    }

}
