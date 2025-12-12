namespace Parseidon.Parser.Grammar.Terminals;

public class TMScopeName : AbstractDefinitionElement
{
    public TMScopeName(String scopeName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ScopeName = scopeName;
    }

    public String ScopeName { get; }
}
