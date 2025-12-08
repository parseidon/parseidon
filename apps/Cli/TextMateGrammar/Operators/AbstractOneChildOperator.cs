using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public abstract class AbstractOneChildOperator : AbstractDefinitionElement
{
    private AbstractDefinitionElement? _element;
    protected AbstractOneChildOperator(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Element = element;
    }

    public AbstractDefinitionElement? Element
    {
        get => _element;
        set
        {
            if (_element != null)
                _element.Parent = null;
            _element = value;
            if (_element != null)
                _element.Parent = this;
        }
    }

    public override bool MatchesVariableText() => Element is null ? base.MatchesVariableText() : Element.MatchesVariableText();

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            Element!.IterateElements(process);
    }

    internal override RegExResult GetRegEx(Grammar grammar)
    {
        return Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
    }

}
