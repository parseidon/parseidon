using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractOneChildOperator : AbstractOperator
{
    private AbstractGrammarElement? _element;
    protected AbstractOneChildOperator(AbstractGrammarElement? element)
    {
        Element = element;
    }

    public AbstractGrammarElement? Element
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

    public override bool IsStatic() => Element is null ? base.IsStatic() : Element.IsStatic();

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if(process(this));
            Element!.IterateElements(process);
    }
}
