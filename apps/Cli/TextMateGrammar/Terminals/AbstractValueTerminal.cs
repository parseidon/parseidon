using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar.Terminals;

public abstract class AbstractValueTerminal : AbstractFinalTerminal
{
    public AbstractValueTerminal(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    public override bool MatchesVariableText() => false;

    public abstract String AsText();

    internal protected override RegExResult GetRegEx(Grammar grammar)
    {
        return new RegExResult(AsText(), Array.Empty<String>());
    }
}
