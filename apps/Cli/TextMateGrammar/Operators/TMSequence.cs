using System.Data.SqlTypes;
using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class TMSequence : AbstractDefinitionElement
{
    private List<AbstractDefinitionElement> _elements = new List<AbstractDefinitionElement>();

    public TMSequence(List<AbstractDefinitionElement> elements, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Elements = elements;
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

    internal override RegExResult GetRegEx(Grammar grammar)
    {
        String regEx = String.Empty;
        String[] captures = Array.Empty<String>();
        foreach (var element in Elements)
        {
            var tempRegEx = element.GetRegEx(grammar);
            regEx = regEx + tempRegEx.RegEx;
            captures = captures.Concat(tempRegEx.Captures).ToArray();
        }
        if (ScopeName is null)
            regEx = $"(?:{regEx})";
        else
        {
            captures = new[] { ScopeName }.Concat(captures).ToArray();
            regEx = $"({regEx})";
        }
        return new RegExResult(regEx, captures);
    }

}
