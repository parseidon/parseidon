using System.Diagnostics;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Block;

public class SimpleRule : AbstractNamedDefinitionElement
{
    public SimpleRule(string name, AbstractGrammarElement definition) : base(name, definition)
    {
    }

    public override String GetReferenceCode(Grammar grammar) => $"CheckRule_{Name}(actualNode, state)";

    public override bool MatchesVariableText() => Definition is DropMarker ? false : Definition.MatchesVariableText();

}
