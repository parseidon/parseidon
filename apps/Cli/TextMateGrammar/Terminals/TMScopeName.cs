using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public class TMScopeName : AbstractDefinitionElement
{
    public TMScopeName(String scopeName, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        ScopeName = scopeName;
    }

    public String ScopeName { get; }
}
