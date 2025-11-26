namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractTwoChildOperator : AbstractDefinitionElement
{
    protected AbstractTwoChildOperator(AbstractDefinitionElement? left, AbstractDefinitionElement? right, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        if ((left == null) || (right == null))
            throw GetException("Missing child element!");
        Left = left!;
        Right = right!;
        Left.Parent = this;
        Right.Parent = this;
    }

    public AbstractDefinitionElement Left { get; }
    public AbstractDefinitionElement Right { get; }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
        {
            Left.IterateElements(process);
            Right.IterateElements(process);
        }
    }
}
