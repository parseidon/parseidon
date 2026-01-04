using System.Linq;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Blocks;

public class Definition : AbstractNamedElement
{
    public Definition(string name, AbstractDefinitionElement definitionElement, IReadOnlyList<ValuePair> valuePairs, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node, List<AbstractMarker> customMarker) : base(name, calcLocation, node)
    {
        _customMarker = customMarker;
        ValuePairs = valuePairs;
        KeyValuePairs = valuePairs.ToDictionary(pair => pair.Name, pair => pair.Value);
        DefinitionElement = definitionElement;
        DefinitionElement.Parent = this;
    }

    private readonly List<AbstractMarker> _customMarker;

    public bool HasMarker<T>() where T : AbstractMarker
    {
        return _customMarker.Any(marker => marker is T);
    }

    public String GetReferenceCode(Grammar grammar) =>
        HasMarker<TreatInlineMarker>()
        ? ToParserCode(grammar)
        : $"CheckDefinition_{Name}(actualNode, state, errorName)";

    public override String ToParserCode(Grammar grammar) => DefinitionElement.ToParserCode(grammar);

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            DefinitionElement.IterateElements(process);
    }

    public AbstractDefinitionElement DefinitionElement { get; }
    public IReadOnlyList<ValuePair> ValuePairs { get; }
    public IReadOnlyDictionary<String, String> KeyValuePairs { get; }
    public Boolean DropDefinition { get => HasMarker<DropMarker>() || HasMarker<TreatInlineMarker>(); }
    public override Boolean MatchesVariableText(Grammar grammar) => !DropDefinition && DefinitionElement.MatchesVariableText(grammar);

}
