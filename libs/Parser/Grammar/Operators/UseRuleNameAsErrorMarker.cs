using Parseidon.Parser.Grammar.Blocks;

namespace Parseidon.Parser.Grammar.Operators;

public class UseDefinitionNameAsErrorMarker : AbstractMarker
{
    public UseDefinitionNameAsErrorMarker(AbstractDefinitionElement? element, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(element, calcLocation, node) { }

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
