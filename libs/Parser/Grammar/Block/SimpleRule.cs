using System.Diagnostics;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Block;

public class SimpleRule : AbstractNamedDefinitionElement
{
    public SimpleRule(string name, AbstractGrammarElement definition, MessageContext messageContext, ASTNode node, List<AbstractMarker> customMarker) : base(name, definition, messageContext, node)
    {
        _customMarker = customMarker;
    }

    private List<AbstractMarker> _customMarker;

    public bool HasMarker<T>() where T : AbstractMarker
    {
        return _customMarker.Any(marker => marker is T);
    }

    public override String GetReferenceCode(Grammar grammar) =>
        HasMarker<TreatInlineMarker>()
        ? ToString(grammar)
        : $"CheckRule_{Name}(actualNode, state)";

    public Boolean DropRule { get => HasMarker<DropMarker>() || HasMarker<TreatInlineMarker>(); }

    public override bool MatchesVariableText() => DropRule ? false : Definition.MatchesVariableText();

}
