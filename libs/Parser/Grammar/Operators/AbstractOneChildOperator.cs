using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractOneChildOperator : AbstractOperator
{
    private AbstractGrammarElement? _element;
    protected AbstractOneChildOperator(AbstractGrammarElement? element)
    {
        Element = element;
    }

    public AbstractGrammarElement? Element {
        get => _element;
        set {
            if (_element != null)
                _element.Parent = null;
            _element = value;
            if (_element != null)
                _element.Parent = this;
        }
    }

    public override void AddUsedRules(List<SimpleRule> rules) => Element?.AddUsedRules(rules);

}
