using System.Diagnostics;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser.Grammar.Block;

public class ValuePair : AbstractNamedElement
{
    public ValuePair(String name, String value, MessageContext messageContext, ASTNode node) : base(name, messageContext, node)
    {
        Value = value;
    }

    public String Value { get; }
    public override bool MatchesVariableText() => false;

}
