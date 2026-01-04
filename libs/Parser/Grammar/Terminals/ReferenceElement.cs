using System.Diagnostics.Contracts;
using Parseidon.Parser.Grammar.Blocks;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Terminals;


public class ReferenceElement : AbstractValueTerminal
{
    public ReferenceElement(String referenceName, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
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

    public override Boolean MatchesVariableText(Grammar grammar)
    {
        if (grammar.FindDefinitionByName(ReferenceName) is Definition referencedDefinition)
            return referencedDefinition.MatchesVariableText(grammar);
        return false;
    }

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        if (grammar.FindDefinitionByName(ReferenceName) is Definition referencedDefinition)
        {
            if (referencedDefinition.KeyValuePairs.TryGetValue(Grammar.TextMatePropertyScope, out var textMatePropertyScope))
            {
                var regEx = referencedDefinition.DefinitionElement.GetRegEx(grammar);
                return new RegExResult($"({regEx.RegEx})", new[] { textMatePropertyScope }.Concat(regEx.Captures).ToArray());
            }
            return referencedDefinition.DefinitionElement.GetRegEx(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }
}
