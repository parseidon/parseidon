using Parseidon.Helper;
using Parseidon.Parser.Grammar.Block;

namespace Parseidon.Parser.Grammar;

public abstract class AbstractGrammarElement
{
    public AbstractGrammarElement? Parent { get; set; }

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
        if (result == null)
            throw new Exception("Can not find grammar!");
        return (Grammar)result;
    }

    protected static string ToLiteral(String valueTextForCompiler, Boolean isEscaped)
    {
        if (isEscaped)
            valueTextForCompiler = valueTextForCompiler.ReplaceAll("\\'", "'").ReplaceAll("\\\"", "\"").ReplaceAll("\\\\", "\\");
        return valueTextForCompiler.FormatLiteral(false).ReplaceAll("\"", "\\\"");
    }

    public virtual String ToString(Grammar grammar) => throw new NotImplementedException($"Not implemented: {this.GetType().Name}.ToString()");

    public virtual Boolean MatchesVariableText() => false;

    internal virtual void IterateElements(Func<AbstractGrammarElement, Boolean> process) => process(this);
}

