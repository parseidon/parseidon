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

    public override string AsText() => ReferenceName;

    public Boolean IsTextMateRule(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is SimpleRule referencedRule)
        {
            return referencedRule.KeyValuePairs.ContainsKey("tmname");
        }
        return false;
    }

    public Boolean Ignore(Grammar grammar)
    {
        if (grammar.FindRuleByName(ReferenceName) is SimpleRule referencedRule)
        {
            return referencedRule.KeyValuePairs.ContainsKey("tmignore");
        }
        return false;
    }

    public override RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after)
    {
        throw new NotImplementedException();
    }


}
