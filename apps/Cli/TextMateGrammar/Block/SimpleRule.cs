using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Operators;

namespace Parseidon.Cli.TextMateGrammar.Block;

public class SimpleRule : AbstractNamedElement
{
    public SimpleRule(string name, AbstractGrammarElement definition, IReadOnlyDictionary<String, String> keyValuePairs, MessageContext messageContext, ASTNode node, List<AbstractMarker> customMarker) : base(name, messageContext, node)
    {
        _customMarker = customMarker;
        KeyValuePairs = keyValuePairs;
        Definition = definition;
        definition.Parent = this;
    }

    private List<AbstractMarker> _customMarker;

    public bool HasMarker<T>() where T : AbstractMarker
    {
        return _customMarker.Any(marker => marker is T);
    }

    public override String ToString(Grammar grammar)
    {
        return $$"""
        "{{Name.ToLower()}}": {
            "match": "{{Definition.ToString(grammar)}}"
        },

        """;
    }

    public AbstractGrammarElement Definition { get; }
    public IReadOnlyDictionary<String, String> KeyValuePairs { get; }
}
