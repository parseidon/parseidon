using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public abstract class AbstractOneChildOperator : AbstractDefinitionElement
{
    private AbstractGrammarElement? _element;
    protected AbstractOneChildOperator(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(messageContext, node)
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
}
