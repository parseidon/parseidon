using System.Diagnostics.Contracts;
using Parseidon.Parser.Grammar.Blocks;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Terminals;


public class TMReferenceElement : ReferenceElement
{
    public TMReferenceElement(String referenceName, MessageContext messageContext, ASTNode node) : base(referenceName, messageContext, node) { }
}
