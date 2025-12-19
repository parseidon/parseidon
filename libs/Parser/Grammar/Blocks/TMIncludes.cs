using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser.Grammar.Blocks;

public class TMIncludes : AbstractDefinitionElement
{
    public TMIncludes(List<ReferenceElement> includes, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Includes = includes;
        foreach (var include in Includes)
            include.Parent = this;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
        {
            foreach (var include in Includes)
                include.IterateElements(process);
        }
    }

    public List<ReferenceElement> Includes { get; }

}
