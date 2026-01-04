namespace Parseidon.Parser.Grammar.Blocks;

public class ValuePair : AbstractNamedElement
{
    public ValuePair(String name, String value, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(name, calcLocation, node)
    {
        Value = value;
    }

    public String Value { get; }
    public override Boolean MatchesVariableText(Grammar grammar) => false;

}
