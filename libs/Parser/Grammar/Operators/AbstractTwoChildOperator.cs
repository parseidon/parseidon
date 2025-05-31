using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractTwoChildOperator : AbstractDefinitionElement
{
    protected AbstractTwoChildOperator(AbstractGrammarElement? left, AbstractGrammarElement? right)
    {
        if ((left == null) || (right == null))
            throw new Exception("Missing child element!");
        Left = left;
        Right = right;
        Left.Parent = this;
        Right.Parent = this;
    }

    public AbstractGrammarElement Left { get; }
    public AbstractGrammarElement Right { get; }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
        {
            Left.IterateElements(process);
            Right.IterateElements(process);
        }
    }
}
