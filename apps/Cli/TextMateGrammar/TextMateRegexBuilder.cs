using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Cli.TextMateGrammar.Operators;
using Parseidon.Cli.TextMateGrammar.Terminals;

namespace Parseidon.Cli.TextMateGrammar;

internal sealed class TextMateRegexBuilder
{
    private readonly Grammar _grammar;
    private readonly Dictionary<String, SimpleRule> _ruleLookup;
    private readonly HashSet<String> _skippableRuleNames;

    public TextMateRegexBuilder(Grammar grammar)
    {
        _grammar = grammar;
        _ruleLookup = grammar.Rules.ToDictionary(rule => rule.Name, StringComparer.OrdinalIgnoreCase);
        _skippableRuleNames = grammar.Rules
            .Where(rule => rule.ShouldSkipInMatch)
            .Select(rule => rule.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public String Build(SimpleRule rule)
    {
        HashSet<String> recursionGuard = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        return BuildRule(rule, recursionGuard);
    }

    private String BuildRule(SimpleRule rule, ISet<String> recursionGuard)
    {
        if (!recursionGuard.Add(rule.Name))
            throw rule.GetException($"Recursive reference '{rule.Name}' can not be converted to a regular expression for TextMate.");
        String result = BuildElement(rule.Definition, recursionGuard);
        recursionGuard.Remove(rule.Name);
        return result;
    }

    private String BuildElement(AbstractDefinitionElement element, ISet<String> recursionGuard)
    {
        switch (element)
        {
            case TextTerminal textTerminal:
                return BuildTextTerminal(textTerminal.Text);
            case NumberTerminal numberTerminal:
                return Regex.Escape(numberTerminal.AsText());
            case BooleanTerminal booleanTerminal:
                return Regex.Escape(booleanTerminal.AsText());
            case RegExTerminal regExTerminal:
                return BuildRegExTerminal(regExTerminal);
            case ReferenceElement referenceElement:
                return BuildReference(referenceElement, recursionGuard);
            case AndOperator andOperator:
                return BuildElement(andOperator.Left, recursionGuard) + BuildElement(andOperator.Right, recursionGuard);
            case OrOperator orOperator:
                return $"(?:{BuildElement(orOperator.Left, recursionGuard)}|{BuildElement(orOperator.Right, recursionGuard)})";
            case OptionalOperator optionalOperator:
                return $"(?:{BuildChild(optionalOperator.Element, recursionGuard, optionalOperator)})?";
            case ZeroOrMoreOperator zeroOrMoreOperator:
                return $"(?:{BuildChild(zeroOrMoreOperator.Element, recursionGuard, zeroOrMoreOperator)})*";
            case OneOrMoreOperator oneOrMoreOperator:
                return $"(?:{BuildChild(oneOrMoreOperator.Element, recursionGuard, oneOrMoreOperator)})+";
            case AbstractOneChildOperator oneChildOperator:
                return BuildChild(oneChildOperator.Element, recursionGuard, oneChildOperator);
            default:
                throw element.GetException($"TextMate regex builder does not support element type '{element.GetType().Name}'.");
        }
    }

    private String BuildChild(AbstractDefinitionElement? child, ISet<String> recursionGuard, AbstractDefinitionElement parent)
    {
        if (child is null)
            throw parent.GetException($"Element '{parent.GetType().Name}' is missing its operand and can not be converted to TextMate regex.");
        return BuildElement(child, recursionGuard);
    }

    private String BuildReference(ReferenceElement referenceElement, ISet<String> recursionGuard)
    {
        if (!_ruleLookup.TryGetValue(referenceElement.ReferenceName, out SimpleRule? referencedRule))
            throw referenceElement.GetException($"Can not find rule '{referenceElement.ReferenceName}' for TextMate generation.");
        if (_skippableRuleNames.Contains(referencedRule.Name))
            return String.Empty;
        return BuildRule(referencedRule, recursionGuard);
    }

    private static String BuildRegExTerminal(RegExTerminal terminal)
    {
        String expression = terminal.RegEx.Trim();
        if (terminal.Quantifier <= 1)
            return expression;
        return $"(?:{expression}){{{terminal.Quantifier}}}";
    }

    private static Boolean IsQuotedLiteral(String value)
    {
        value = value.Trim();
        return value.Length >= 2 && ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"'));
    }

    private static String BuildTextTerminal(String value)
    {
        if (IsQuotedLiteral(value))
            return Regex.Escape(DecodeLiteral(value));
        return value.Trim();
    }

    private static String DecodeLiteral(String literal)
    {
        literal = literal.Trim();
        Char quote = literal[0];
        if (literal[^1] != quote)
            throw new FormatException("Literal is not properly closed.");
        String inner = literal.Substring(1, literal.Length - 2);
        StringBuilder builder = new StringBuilder(inner.Length);
        for (Int32 index = 0; index < inner.Length; index++)
        {
            Char current = inner[index];
            if (current != '\\')
            {
                builder.Append(current);
                continue;
            }
            if (index + 1 >= inner.Length)
            {
                builder.Append('\\');
                break;
            }
            Char next = inner[++index];
            switch (next)
            {
                case '\\':
                    builder.Append('\\');
                    break;
                case '\'':
                    builder.Append('\'');
                    break;
                case '"':
                    builder.Append('"');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case '0':
                    builder.Append('\0');
                    break;
                case 'a':
                    builder.Append('\a');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'v':
                    builder.Append('\v');
                    break;
                case 'u':
                    if (index + 4 >= inner.Length)
                        throw new FormatException("Invalid unicode escape sequence in literal.");
                    String hex = inner.Substring(index + 1, 4);
                    builder.Append((Char)Int32.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    index += 4;
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }
        return builder.ToString();
    }
}
