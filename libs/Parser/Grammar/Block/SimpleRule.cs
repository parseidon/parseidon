using Humanizer;

namespace Parseidon.Parser.Grammar.Block;

public class SimpleRule : AbstractNamedDefinitionElement
{
    public SimpleRule(string name, AbstractGrammarElement definition) : base(name, definition)
    {
    }

    public override String GetReferenceCode(Grammar grammar) => $"CheckRule_{Name}(actualNode, state)";

    public override void AddUsedRules(List<SimpleRule> rules) 
    {
        if(rules.IndexOf(this) < 0)
        {
            rules.Add(this);
            if(Definition != null)
                Definition.AddUsedRules(rules);
        }
    }

    public override String GetEventName() => $"On{Name.Humanize().Dehumanize()}";


}
