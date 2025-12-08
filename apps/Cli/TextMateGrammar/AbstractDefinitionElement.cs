using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    internal virtual RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult("", Array.Empty<String>());
    }

    internal record RegExResult(String RegEx, String[] Captures);
}
