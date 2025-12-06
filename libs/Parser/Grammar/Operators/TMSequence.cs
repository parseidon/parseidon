namespace Parseidon.Parser.Grammar.Operators;

public class TMSequence : AbstractDefinitionElement
{
    private List<AbstractDefinitionElement> _elements = new List<AbstractDefinitionElement>();

    public TMSequence(List<AbstractDefinitionElement> elements, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Elements = elements;
    }

    public List<AbstractDefinitionElement> Elements
    {
        get => _elements;
        set
        {
            SetElementsParent(null);
            _elements = value;
            SetElementsParent(this);
        }
    }

    private void SetElementsParent(AbstractDefinitionElement? parent)
    {
        foreach (var element in _elements)
            element.Parent = parent;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            foreach (var element in Elements)
                element.IterateElements(process);
    }
}
