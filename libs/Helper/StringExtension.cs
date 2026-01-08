using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Parseidon.Helper;

public static class StringExtensions
{
    public static string ReplaceAt(this String source, Int32 index, Int32 length, String replacement)
    {
        if (index < 0 || index > source.Length - length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return source.Substring(0, index) + replacement + source.Substring(index + length);
    }

    public static String ReplaceAll(this String input, (String Search, String Replace)[] rules)
    {
        if (input.Length == 0 || rules.Length == 0)
            return input;

        // Build a trie for all search strings so we can scan the input once and
        // find the first matching rule (by original order) at each position.
        var root = new TrieNode();
        for (Int32 ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            var search = rules[ruleIndex].Search;
            if (String.IsNullOrEmpty(search))
                continue;

            var node = root;
            foreach (Char c in search)
            {
                if (!node.Children.TryGetValue(c, out var next))
                {
                    next = new TrieNode();
                    node.Children.Add(c, next);
                }
                node = next;
            }

            // Keep the earliest rule for identical search strings.
            if (!node.RuleIndex.HasValue || ruleIndex < node.RuleIndex.Value)
            {
                node.RuleIndex = ruleIndex;
                node.MatchLength = search.Length;
            }
        }

        var builder = new StringBuilder(input.Length);
        Int32 i = 0;
        while (i < input.Length)
        {
            var node = root;
            Int32 bestRuleIndex = Int32.MaxValue;
            Int32 bestLength = 0;

            for (Int32 j = i; j < input.Length; j++)
            {
                var c = input[j];
                if (!node.Children.TryGetValue(c, out node))
                    break;

                if (node.RuleIndex.HasValue && node.RuleIndex.Value < bestRuleIndex)
                {
                    bestRuleIndex = node.RuleIndex.Value;
                    bestLength = node.MatchLength;
                }
            }

            if (bestLength > 0)
            {
                builder.Append(rules[bestRuleIndex].Replace ?? String.Empty);
                i += bestLength;
            }
            else
            {
                builder.Append(input[i]);
                i++;
            }
        }

        return builder.ToString();
    }

    public static Boolean ContainsNewLine(this String input)
    {
        foreach (Char c in input)
        {
            if (c == '\r' || c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                return true;
        }
        return false;
    }

    public static String Unescape(this String value)
    {
        var rules = new (String Search, String Replace)[]
        {
            ("\\'", "'"),
            ("\\\"", "\""),
            ("\\\\", "\\"),
            ("\\0", "\0"),
            ("\\a", "\a"),
            ("\\b", "\b"),
            ("\\f", "\f"),
            ("\\n", "\n"),
            ("\\r", "\r"),
            ("\\t", "\t"),
            ("\\v", "\v")
        };
        return value.ReplaceAll(rules);
    }

    public static String FormatLiteral(this String value, Boolean useQuotes)
    {
        const Char quote = '"';

        var builder = new StringBuilder();

        if (useQuotes)
            builder.Append(quote);

        for (Int32 i = 0; i < value.Length; i++)
        {
            Char c = value[i];
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Surrogate)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                if (category == UnicodeCategory.Surrogate)
                {
                    builder.Append($"\\u{((int)c).ToString("x4")}");
                }
                else if (NeedsEscaping(category))
                {
                    var unicode = char.ConvertToUtf32(value, i);
                    builder.Append($"\\U{unicode.ToString("x8")}");
                    i++;
                }
                else
                {
                    builder.Append(c);
                    builder.Append(value[++i]);
                }
            }
            else if (TryReplaceChar(c, out var replaceWith))
            {
                builder.Append(replaceWith);
            }
            else if (useQuotes && c == quote)
            {
                builder.Append('\\');
                builder.Append(quote);
            }
            else
            {
                builder.Append(c);
            }
        }
        if (useQuotes)
            builder.Append(quote);

        return builder.ToString();
    }

    private static Boolean NeedsEscaping(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.Control:
            case UnicodeCategory.OtherNotAssigned:
            case UnicodeCategory.ParagraphSeparator:
            case UnicodeCategory.LineSeparator:
            case UnicodeCategory.Surrogate:
                return true;
            default:
                return false;
        }
    }

    private sealed class TrieNode
    {
        public Dictionary<Char, TrieNode> Children { get; } = new();
        public Int32? RuleIndex { get; set; }
        public Int32 MatchLength { get; set; }
    }

    private static Boolean TryReplaceChar(Char c, out String? replaceWith)
    {
        replaceWith = null;
        switch (c)
        {
            case '\\':
                replaceWith = "\\\\";
                break;
            case '\0':
                replaceWith = "\\0";
                break;
            case '\a':
                replaceWith = "\\a";
                break;
            case '\b':
                replaceWith = "\\b";
                break;
            case '\f':
                replaceWith = "\\f";
                break;
            case '\n':
                replaceWith = "\\n";
                break;
            case '\r':
                replaceWith = "\\r";
                break;
            case '\t':
                replaceWith = "\\t";
                break;
            case '\v':
                replaceWith = "\\v";
                break;
        }

        if (replaceWith != null)
        {
            return true;
        }

        if (NeedsEscaping(CharUnicodeInfo.GetUnicodeCategory(c)))
        {
            replaceWith = $"\\u{((int)c).ToString("x4")}";
            return true;
        }

        return false;
    }

    public static String TrimLineEndWhitespace(this String input)
    {
        return Regex.Replace(input, @"[ \t]+(?=\r?$)", String.Empty, RegexOptions.Multiline);
    }

    public static String Indent(this String text, Int32 level = 1)
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
}
