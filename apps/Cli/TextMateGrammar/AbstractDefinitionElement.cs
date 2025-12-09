using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    internal protected virtual RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult("", Array.Empty<String>());
    }

    internal protected record RegExResult(String RegEx, String[] Captures);
}
