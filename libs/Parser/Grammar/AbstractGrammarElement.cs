using Parseidon.Helper;

namespace Parseidon.Parser.Grammar;

public abstract class AbstractGrammarElement
{
    public AbstractGrammarElement(MessageContext messageContext, ASTNode node)
    {
        if (messageContext is null) throw new ArgumentNullException(nameof(messageContext));
        if (node is null) throw new ArgumentNullException(nameof(node));
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

    protected String Indent(String text, Int32 level = 1)
    {
        if ((text.Length > 0) && (text[text.Length - 1] == '\n'))
            text = text.Substring(0, text.Length - 1);
        String result = "";
        foreach (String line in text.Split('\n'))
        {
            result += (new String(' ', 4 * level)) + line + "\n";
        }
        return result.Substring(0, result.Length - 1);
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

    public virtual Boolean MatchesVariableText() => false;

    internal virtual void IterateElements(Func<AbstractGrammarElement, Boolean> process) => process(this);
}

