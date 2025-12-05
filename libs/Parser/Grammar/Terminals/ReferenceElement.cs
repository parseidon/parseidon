using System.Diagnostics.Contracts;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Terminals;


public class ReferenceElement : AbstractValueTerminal
{
    public ReferenceElement(String referenceName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ReferenceName = referenceName;
    }

    public String ReferenceName { get; }

    public override String ToString(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is Definition referencedRule)
        {
            if (TreatReferenceInline)
                return referencedRule.ToString(grammar);
            return referencedRule.GetReferenceCode(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }

    public override string AsText() => ReferenceName;

    public Boolean TreatReferenceInline { get => Parent is TreatInlineMarker; }
}
