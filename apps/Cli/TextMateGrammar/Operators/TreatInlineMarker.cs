using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Operators;

public class TreatInlineMarker : AbstractMarker
{
    public TreatInlineMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

}
