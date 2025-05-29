using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public abstract class AbstractTwoChildOperator : AbstractDefinitionElement
{
    protected AbstractTwoChildOperator(AbstractGrammarElement? left, AbstractGrammarElement? right)
    {
        if((left == null) || (right == null))
            throw new Exception("Missing child element!");
        Left = left;
        Right = right;
        Left.Parent = this;
        Right.Parent = this;
    }

    public AbstractGrammarElement Left { get; }
    public AbstractGrammarElement Right { get; }

    // public override void AddUsedTerminals(List<AbstractTerminal> terminals)
    // {
    //     Left.AddUsedTerminals(terminals);
    //     Right.AddUsedTerminals(terminals);
    // }

    public override void AddUsedRules(List<SimpleRule> rules)
    {
        Left.AddUsedRules(rules);
        Right.AddUsedRules(rules);
    }
    
    // public override void AddVirtualActions(List<VirtualAction> virtualActions) 
    // {
    //     Left.AddVirtualActions(virtualActions);
    //     Right.AddVirtualActions(virtualActions);
    // }

}
