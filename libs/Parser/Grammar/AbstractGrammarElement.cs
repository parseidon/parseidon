using Parseidon.Helper;

namespace Parseidon.Parser.Grammar;

public abstract class AbstractGrammarElement
{
    public AbstractGrammarElement(Func<Int32, (UInt32, UInt32)> calcLocation, ASTNode node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        CalcLocation = calcLocation;
        Node = node;
    }

    public ASTNode Node { get; }
    public AbstractGrammarElement? Parent { get; set; }
    public Func<Int32, (UInt32, UInt32)> CalcLocation { get; }

    public GrammarException GetException(String message)
    {
        (UInt32 row, UInt32 column) = CalcLocation(Node!.Position);
        return new GrammarException(message, row, column);
    }

    protected Grammar GetGrammar()
    {
        AbstractGrammarElement? result = this;
        while (!(result is Grammar) && (result != null))
            result = result.Parent;
        if (result is null)
            throw GetException("Can not find grammar!");
        return (Grammar)result!;
    }

    protected static string ToLiteral(String value, Boolean isEscaped)
    {
        if (isEscaped)
            value = value.Unescape();
        return value.FormatLiteral(false).ReplaceAll(new (String Search, String Replace)[] { ("\"", "\\\"") });
    }

    public virtual String ToParserCode(Grammar grammar) => throw new NotImplementedException($"Not implemented: {this.GetType().Name}.ToString()");

    public virtual Boolean MatchesVariableText(Grammar grammar) => false;

    internal virtual void IterateElements(Func<AbstractGrammarElement, Boolean> process) => process(this);
}

