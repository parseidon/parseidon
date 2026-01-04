namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractOneChildOperator : AbstractDefinitionElement
{
    private AbstractDefinitionElement? _element;
    protected AbstractOneChildOperator(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
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

    public override Boolean MatchesVariableText(Grammar grammar) => Element is null ? base.MatchesVariableText(grammar) : Element.MatchesVariableText(grammar);

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            Element!.IterateElements(process);
    }

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        return Element?.GetRegEx(grammar) ?? base.GetRegEx(grammar);
    }
}
