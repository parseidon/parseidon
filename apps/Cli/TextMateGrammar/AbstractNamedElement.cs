using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public abstract class AbstractNamedElement : AbstractGrammarElement
{
    public AbstractNamedElement(String name, MessageContext messageContext, ASTNode node) : base(messageContext, node)
    {
        Name = name;
    }
    public String Name { get; }
}
