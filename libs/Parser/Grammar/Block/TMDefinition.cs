using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Block;

public class TMDefinition : AbstractNamedElement
{
    public TMDefinition(String name, String? scopeName, TMSequence beginSequence, TMIncludes? includes, TMSequence? endSequence, MessageContext messageContext, ASTNode node) : base(name, messageContext, node)
    {
        ScopeName = scopeName;
        BeginSequence = beginSequence;
        Includes = includes;
        if (Includes is not null)
            Includes.Parent = this;
        EndSequence = endSequence;
        if (EndSequence is not null)
            EndSequence.Parent = this;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
        {
            BeginSequence.IterateElements(process);
            if (EndSequence is not null)
                EndSequence.IterateElements(process);
        }
    }

    public String? ScopeName { get; }
    public TMSequence BeginSequence { get; }
    public TMSequence? EndSequence { get; }
    public TMIncludes? Includes { get; }


}
