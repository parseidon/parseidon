using System.Text;
using Humanizer;
using Parseidon.Helper;
using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Terminals;
using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Cli.TextMateGrammar.Operators;

namespace Parseidon.Cli.TextMateGrammar;

public class Grammar : AbstractNamedElement
{
    public Grammar(List<SimpleRule> rules, List<ValuePair> options, MessageContext messageContext, ASTNode node) : base("", messageContext, node)
    {
        Rules = rules;
        Options = options;
        CheckDuplicatedRules(Rules);
        Rules.ForEach((element) => element.Parent = this);
    }

    public List<SimpleRule> Rules { get; }
    public List<ValuePair> Options { get; }

    public override String ToString(Grammar grammar)
    {
        return
            $$"""
            {
                "scopeName": "",
                "fileTypes": [],
                "patterns": [
                ]
            }
            """.TrimLineEndWhitespace();
    }

    public override String ToString() => ToString(this);

    public SimpleRule? FindRuleByName(String name)
    {
        List<SimpleRule> rules = new List<SimpleRule>();
        foreach (SimpleRule element in Rules)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return element;
        return null;
    }

    public void CheckDuplicatedRules(List<SimpleRule> rules)
    {
        HashSet<String> existingRules = new HashSet<String>(StringComparer.InvariantCultureIgnoreCase);
        foreach (SimpleRule rule in rules)
            if (!existingRules.Add(rule.Name))
                throw rule.GetException($"Rule '{rule.Name}' already exists!");
    }

    public Int32 GetElementIdOf(AbstractNamedElement element)
    {
        if ((element is SimpleRule) && (Rules.IndexOf((SimpleRule)element) >= 0))
            return Rules.IndexOf((SimpleRule)element);
        throw GetException($"Can not find identifier '{element.Name}'!");
    }

    public SimpleRule GetRootRule()
    {
        String? axiomName = GetOptionValue("rootnode");
        if (String.IsNullOrWhiteSpace(axiomName))
            throw GetException("Grammar must have axiom option!");
        SimpleRule? rule = FindRuleByName(axiomName);
        if (rule is null)
            throw GetException($"Can not find axiom option '{axiomName}'!");
        return rule;
    }

    private String GetOptionValue(String key)
    {
        foreach (ValuePair value in Options)
        {
            if (value.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.Value;
        }
        throw GetException($"Can not find option '{key}'!");
    }

}
