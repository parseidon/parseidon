using Parseidon.Helper;
using Parseidon.Parser;

namespace Parseidon.Cli.TextMateGrammar;

public abstract class AbstractGrammarElement
{
    public AbstractGrammarElement(MessageContext messageContext, ASTNode node)
    {
        if (messageContext is null)
            throw new ArgumentNullException(nameof(messageContext));
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        MessageContext = messageContext;
        Node = node;
    }

    public MessageContext MessageContext { get; }
    public ASTNode Node { get; }
    public AbstractGrammarElement? Parent { get; set; }

    public GrammarException GetException(String message)
    {
        (UInt32 row, UInt32 column) = MessageContext!.CalculateLocation(Node!.Position);
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

    protected static string ToLiteral(String valueTextForCompiler, Boolean isEscaped)
    {
        if (isEscaped)
            valueTextForCompiler = valueTextForCompiler.ReplaceAll("\\'", "'").ReplaceAll("\\\"", "\"").ReplaceAll("\\\\", "\\");
        return valueTextForCompiler.FormatLiteral(false).ReplaceAll("\"", "\\\"");
    }

    public virtual String ToString(Grammar grammar) => throw new NotImplementedException($"Not implemented: {this.GetType().Name}.ToString()");
}

