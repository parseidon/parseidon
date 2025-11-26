using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;

namespace Parseidon.Cli.TextMateGrammar.Terminals;


public class ReferenceElement : AbstractValueTerminal
{
    public ReferenceElement(String referenceName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ReferenceName = referenceName;
    }

    public String ReferenceName { get; }

    public override String ToString(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is SimpleRule referencedRule)
            return referencedRule.ToString(grammar);
        throw GetException($"Can not find element '{ReferenceName}'");
    }

    public override string AsText() => ReferenceName;
}
