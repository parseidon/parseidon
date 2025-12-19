namespace Parseidon.Parser.Grammar.Operators;

public class TMSequence : AbstractDefinitionElement
{
    private List<AbstractDefinitionElement> _elements = new List<AbstractDefinitionElement>();

    public TMSequence(List<AbstractDefinitionElement> elements, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Elements = elements;
        foreach (var element in Elements)
            element.Parent = this;
    }

    public String? ScopeName { get; set; }

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

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        String[] captures = Elements
            .Select(element => element.GetRegEx(grammar).Captures)
            .SelectMany(c => c)
            .ToArray();
        String regEx = String.Concat(Elements.Select(element => element.GetRegEx(grammar).RegEx));
        if (ScopeName is null)
            regEx = $"(?:{regEx})";
        else
        {
            String grammarSuffix = grammar.GetGrammarSuffix();
            String extendedScopeName = Grammar.AppendGrammarSuffix(ScopeName, grammarSuffix) ?? ScopeName;
            captures = new[] { extendedScopeName }.Concat(captures).ToArray();
            regEx = $"({regEx})";
        }
        return new RegExResult(regEx, captures);
    }
}
