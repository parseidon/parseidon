using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Cli.TextMateGrammar.Operators;

namespace Parseidon.Cli.TextMateGrammar.Terminals;


public class ReferenceElement : AbstractValueTerminal
{
    public ReferenceElement(String referenceName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ReferenceName = referenceName;
    }

    public String ReferenceName { get; }

    public override String ToParserCode(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is Definition referencedRule)
        {
            if (TreatReferenceInline)
                return referencedRule.ToParserCode(grammar);
            return referencedRule.GetReferenceCode(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }

    public override string AsText() => ReferenceName;

    public Boolean TreatReferenceInline { get => Parent is TreatInlineMarker; }

    internal override RegExResult GetRegEx(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is Definition referencedRule)
        {
            return referencedRule.DefinitionElement.GetRegEx(grammar);
        }
        throw GetException($"Can not find element '{ReferenceName}'");
    }
}
