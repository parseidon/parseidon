using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar.Operators;

public class UseRuleNameAsErrorMarker : AbstractMarker
{
    public UseRuleNameAsErrorMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }


    public override String ToString(Grammar grammar)
    {
        SimpleRule? rule = GetRule();
        String errorName = rule.KeyValuePairs.TryGetValue("ErrorName", out String temp) ? $"\"{temp}\"" : $"\"{rule?.Name}\"" ?? "errorName";
        String result = "";
        result += $"SetErrorName(actualNode, state, {errorName},\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => Element?.MatchesVariableText() ?? false;

}
