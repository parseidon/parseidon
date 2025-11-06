using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar;


public class ReferenceElement : AbstractGrammarElement
{
    public ReferenceElement(String referenceName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ReferenceName = referenceName;
    }

    public String ReferenceName { get; }

    public override String ToString(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is SimpleRule referencedRule)
            return referencedRule.GetReferenceCode(grammar);
        throw GetException($"Can not find element '{ReferenceName}'");
    }

    public override Boolean MatchesVariableText() => false;

}
