namespace Parseidon.Parser.Grammar.Operators;

public class TreatInlineMarker : AbstractMarker
{
    public TreatInlineMarker(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

}
