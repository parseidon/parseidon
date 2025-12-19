namespace Parseidon.Parser.Grammar.Terminals;

public class TMScopeName : AbstractDefinitionElement
{
    public TMScopeName(String scopeName, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        ScopeName = scopeName;
    }

    public String ScopeName { get; }
}
