using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar.Block;

public class Definition : AbstractNamedElement
{
    public Definition(string name, AbstractDefinitionElement definitionElement, IReadOnlyDictionary<String, String> keyValuePairs, MessageContext messageContext, ASTNode node, List<AbstractMarker> customMarker) : base(name, messageContext, node)
    {
        _customMarker = customMarker;
        KeyValuePairs = keyValuePairs;
        DefinitionElement = definitionElement;
        DefinitionElement.Parent = this;
    }

    private List<AbstractMarker> _customMarker;

    public bool HasMarker<T>() where T : AbstractMarker
    {
        return _customMarker.Any(marker => marker is T);
    }

    public String GetReferenceCode(Grammar grammar) =>
        HasMarker<TreatInlineMarker>()
        ? ToString(grammar)
        : $"CheckRule_{Name}(actualNode, state, errorName)";

    public override String ToString(Grammar grammar) => DefinitionElement.ToString(grammar);

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            DefinitionElement.IterateElements(process);
    }

    public AbstractDefinitionElement DefinitionElement { get; }
    public IReadOnlyDictionary<String, String> KeyValuePairs { get; }
    public Boolean DropRule { get => HasMarker<DropMarker>() || HasMarker<TreatInlineMarker>(); }
    public override bool MatchesVariableText() => DropRule ? false : DefinitionElement.MatchesVariableText();

}
