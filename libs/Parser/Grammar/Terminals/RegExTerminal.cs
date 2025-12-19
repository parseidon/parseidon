namespace Parseidon.Parser.Grammar.Terminals;

public class RegExTerminal : AbstractDefinitionElement
{
    public RegExTerminal(String regEx, Int32 quantifier, Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node)
    {
        RegEx = regEx;
        Quantifier = quantifier;
        if (RegEx.Length == 0)
            throw new ArgumentException("");
    }

    public String RegEx { get; }
    public Int32 Quantifier { get; }

    public override String ToParserCode(Grammar grammar) => $"CheckRegEx(actualNode, state, errorName, \"{ToLiteral(RegEx, false).Trim()}\", {Quantifier})";

    public override bool MatchesVariableText() => true;

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult($"{RegEx.Trim()}{{{Quantifier}}}", Array.Empty<String>());
    }
}
