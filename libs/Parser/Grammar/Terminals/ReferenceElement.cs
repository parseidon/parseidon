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

    public override String ToParserCode(Grammar grammar)
    {
        if (grammar.FindDefinitionByName(ReferenceName) is Definition referencedDefinition)
        {
            if (TreatReferenceInline)
                return referencedDefinition.ToParserCode(grammar);
            return referencedDefinition.GetReferenceCode(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }

    public override string AsText() => ReferenceName;

    public Boolean TreatReferenceInline { get => Parent is TreatInlineMarker; }
}
