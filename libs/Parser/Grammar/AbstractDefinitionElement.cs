namespace Parseidon.Parser.Grammar;

public class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node) : base(calcLocation, node) { }

    internal protected virtual RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult("", Array.Empty<String>());
    }

    internal protected record RegExResult
    {
        public RegExResult(String regEx, String[] captures)
        {
            RegEx = regEx;
            Captures = captures;
        }

        public String RegEx { get; }
        public String[] Captures { get; }
    }
}
