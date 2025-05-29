namespace Parseidon.Parser.Grammar;

public abstract class AbstractNamedElement : AbstractGrammarElement
{
    public AbstractNamedElement(String name)
    {
        Name = name;
    }    
    public String Name { get; }

    public virtual String GetEventName() => "";
}
