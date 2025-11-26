using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public abstract class AbstractTwoChildOperator : AbstractDefinitionElement
{
    protected AbstractTwoChildOperator(AbstractGrammarElement? left, AbstractGrammarElement? right, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        if ((left == null) || (right == null))
            throw GetException("Missing child element!");
        Left = left!;
        Right = right!;
        Left.Parent = this;
        Right.Parent = this;
    }

    public AbstractGrammarElement Left { get; }
    public AbstractGrammarElement Right { get; }
}
