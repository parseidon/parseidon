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

    // public override void AddUsedTerminals(List<AbstractTerminal> terminals) => Element.AddUsedTerminals(terminals);

    public override void AddUsedRules(List<SimpleRule> rules) => Element.AddUsedRules(rules);

    // public override void AddVirtualActions(List<VirtualAction> virtualActions) => Element.AddVirtualActions(virtualActions);

    public override String ToString(Grammar grammar)
    {
        return Element.ToString(grammar);
    }
}
