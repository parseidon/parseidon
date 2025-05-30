namespace Parseidon.Parser.Grammar.Block;

public abstract class AbstractNamedDefinitionElement : AbstractNamedElement
{
    public AbstractNamedDefinitionElement(String name, AbstractGrammarElement definition) : base(name)
    {
        Definition = definition;
        Definition.Parent = this;
    }
    public AbstractGrammarElement Definition { get; }

    public override String ToString(Grammar grammar) => Definition.ToString(grammar);

    public abstract String GetReferenceCode(Grammar grammar);

    public override bool MatchesVariableText() => Definition.MatchesVariableText();
    
    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if(process(this))
            Definition.IterateElements(process);
    }    
}
