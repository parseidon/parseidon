using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar;


public class ReferenceElement : AbstractGrammarElement
{
    public ReferenceElement(String referenceName)
    {
        ReferenceName = referenceName;
    }

    public String ReferenceName { get; }

    public override String ToString(Grammar grammar)
    {
        List<SimpleRule> rules = grammar.FindRulesByName(ReferenceName);
        if (rules.Count > 0)
        {
            if (rules.Count == 1)
                return rules[0].GetReferenceCode(grammar);
            OrOperator orOperator = new OrOperator(rules[0], rules[1]);
            return orOperator.ToString(grammar);
            // throw new NotImplementedException("Multiple Rules with one Name");
        }
        throw new Exception($"Can not find element '{ReferenceName}'");
    }

    public override void AddUsedRules(List<SimpleRule> rules)
    {
       List<SimpleRule> findRules = GetGrammar().FindRulesByName(ReferenceName);
        if (findRules.Count > 0)
            foreach (SimpleRule rule in findRules)
                rule.AddUsedRules(rules);
    }
}
