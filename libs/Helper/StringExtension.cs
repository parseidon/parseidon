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
        var stringBuilder = new System.Text.StringBuilder(input.Length);

        Int32 i = 0;
        while (i < input.Length)
        {
            Boolean matched = false;
            foreach (var rule in rules)
            {
                var search = rule.Search;
                if (string.IsNullOrEmpty(search))
                    continue;
                Int32 len = search.Length;
                if (i + len > input.Length)
                    continue;
                Boolean equal = true;
                for (Int32 j = 0; j < len; j++)
                {
                    if (input[i + j] != search[j])
                    {
                        equal = false;
                        break;
                    }
                }
                if (equal)
                {
                    stringBuilder.Append(rule.Replace ?? String.Empty);
                    i += len;
                    matched = true;
                    break;
                }
            }
            if (!matched)
            {
                stringBuilder.Append(input[i]);
                i++;
            }
        }

        return stringBuilder.ToString();
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
}
