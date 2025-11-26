namespace Parseidon.Parser.Grammar;

public class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    private sealed record Pattern
    {
        public String match = String.Empty;
    }

    public virtual Object GetPattern() => new Pattern() { match = "" };

}
