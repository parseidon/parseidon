using System.Diagnostics.Contracts;
using Parseidon.Parser.Grammar.Blocks;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Terminals;


public class TMReferenceElement : ReferenceElement
{
    public TMReferenceElement(String referenceName, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(referenceName, calcLocation, node) { }
}
