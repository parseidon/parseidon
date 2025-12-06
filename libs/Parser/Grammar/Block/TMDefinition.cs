using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Block;

public class TMDefinition : AbstractNamedElement
{
    public TMDefinition(string name, TMSequence beginSequence, TMSequence? endSequence, MessageContext messageContext, ASTNode node) : base(name, messageContext, node)
    {
        BeginSequence = beginSequence;
        BeginSequence.Parent = this;
        EndSequence = endSequence;
        if (EndSequence is not null)
            EndSequence.Parent = this;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            BeginSequence.IterateElements(process);
        // if (process(this))
        //     BeginSequence.IterateElements(process);
        if (process(this))
            EndSequence?.IterateElements(process);
    }

    public TMSequence BeginSequence { get; }
    public TMSequence? EndSequence { get; }

}
