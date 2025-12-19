namespace Parseidon.Parser.Grammar;

public abstract class AbstractNamedElement : AbstractGrammarElement
{
    public AbstractNamedElement(String name, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        Name = name;
    }
    public String Name { get; }
}
