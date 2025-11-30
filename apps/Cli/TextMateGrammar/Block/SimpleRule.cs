using System.Diagnostics.CodeAnalysis;
using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Operators;
using Parseidon.Cli.TextMateGrammar.Terminals;

namespace Parseidon.Cli.TextMateGrammar.Block;

public class SimpleRule : AbstractNamedElement
{
    public SimpleRule(string name, AbstractDefinitionElement definition, IReadOnlyDictionary<String, String> keyValuePairs, MessageContext messageContext, ASTNode node, List<AbstractMarker> customMarker) : base(name, messageContext, node)
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

    public AbstractDefinitionElement Definition { get; }
    public IReadOnlyDictionary<String, String> KeyValuePairs { get; }

    public Boolean HasTextMateName => KeyValuePairs.ContainsKey("tmname");

    public Boolean TryGetTextMateName([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out String? value) => KeyValuePairs.TryGetValue("tmname", out value);

    public Boolean IsIgnored => KeyValuePairs.ContainsKey("tmignore");

    public Boolean IsDropRule => HasMarker<DropMarker>();

    public Boolean ShouldSkipInMatch => !HasTextMateName && (IsIgnored || IsDropRule);

    public String GetRepositoryKey() => Name.ToLowerInvariant();

    public IEnumerable<String> GetReferencedRuleNames()
    {
        HashSet<String> references = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (ReferenceElement reference in EnumerateDefinition(Definition).OfType<ReferenceElement>())
            references.Add(reference.ReferenceName);
        return references;
    }

    private static IEnumerable<AbstractDefinitionElement> EnumerateDefinition(AbstractDefinitionElement element)
    {
        yield return element;
        switch (element)
        {
            case AbstractTwoChildOperator twoChildOperator:
                foreach (AbstractDefinitionElement child in EnumerateDefinition(twoChildOperator.Left))
                    yield return child;
                foreach (AbstractDefinitionElement child in EnumerateDefinition(twoChildOperator.Right))
                    yield return child;
                break;
            case AbstractOneChildOperator oneChildOperator when oneChildOperator.Element is not null:
                foreach (AbstractDefinitionElement child in EnumerateDefinition(oneChildOperator.Element))
                    yield return child;
                break;
        }
    }

}
