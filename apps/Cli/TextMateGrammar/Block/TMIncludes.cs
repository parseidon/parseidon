using Parseidon.Parser;

using Parseidon.Cli.TextMateGrammar.Operators;
using System.Text.Json.Serialization;
using System.IO.Pipelines;
using Parseidon.Cli.TextMateGrammar.Terminals;

namespace Parseidon.Cli.TextMateGrammar.Block;

public class TMIncludes : AbstractDefinitionElement
{
    public TMIncludes(List<ReferenceElement> includes, MessageContext messageContext, ASTNode node) : base(messageContext, node)
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
