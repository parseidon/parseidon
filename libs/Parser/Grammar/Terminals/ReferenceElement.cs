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

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        if (grammar.FindDefinitionByName(ReferenceName) is Definition referencedDefinition)
        {
            if (referencedDefinition.KeyValuePairs.ContainsKey(Grammar.TextMatePropertyScope))
            {
                var regEx = referencedDefinition.DefinitionElement.GetRegEx(grammar);
                return new RegExResult($"({regEx.RegEx})", new[] { referencedDefinition.KeyValuePairs[Grammar.TextMatePropertyScope] }.Concat(regEx.Captures).ToArray());
            }
            return referencedDefinition.DefinitionElement.GetRegEx(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }
}
