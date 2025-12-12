using Parseidon.Parser.Grammar.Blocks;

namespace Parseidon.Parser.Grammar.Operators;

public class UseDefinitionNameAsErrorMarker : AbstractMarker
{
    public UseDefinitionNameAsErrorMarker(AbstractDefinitionElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }

    public override String ToParserCode(Grammar grammar)
    {
        Definition definition = GetDefinition();
        String errorName = definition.KeyValuePairs.TryGetValue(Grammar.GrammarPropertyErrorName, out String temp) ? $"\"{temp}\"" : $"\"{definition.Name}\"";
        String result = "";
        result += $"SetErrorName(actualNode, state, {errorName},\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToParserCode(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => Element?.MatchesVariableText() ?? false;

}
