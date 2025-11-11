namespace Parseidon.Parser.Grammar.Operators;

public class UseRuleNameAsErrorMarker : AbstractMarker
{
    public UseRuleNameAsErrorMarker(AbstractGrammarElement? element, MessageContext messageContext, ASTNode node) : base(element, messageContext, node) { }


    public override String ToString(Grammar grammar)
    {
        String result = "";
        result += $"SetErrorName(actualNode, state, \"{GetRule()?.Name ?? "errorName"}\",\n";
        result += Indent($"(actualNode, errorName) => {Element?.ToString(grammar)}") + "\n";
        result += ")";
        return result;
    }

    public override bool MatchesVariableText() => Element?.MatchesVariableText() ?? false;

}
