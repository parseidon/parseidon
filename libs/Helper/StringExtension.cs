using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Parseidon.Helper;

public static class StringExtensions
{
    public static string ReplaceAt(this String source, Int32 index, Int32 length, String replacement)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        if (index < 0 || index > source.Length - length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return source.Substring(0, index) + replacement + source.Substring(index + length);
    }

    public static String ReplaceAll(this String input, String replaceThis, String withThis)
    {
        Int32 position = 0;
        while (position <= (input.Length - replaceThis.Length))
        {
            if (((position + replaceThis.Length) <= input.Length) && (input.Substring(position, replaceThis.Length) == replaceThis))
            {
                input = input.ReplaceAt(position, replaceThis.Length, withThis);
                position += withThis.Length - 1;
            }
            position++;
        }
        return input;
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

    public static String FormatLiteral(this String value, Boolean useQuotes)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        const Char quote = '"';

        var builder = new StringBuilder();

        if (useQuotes)
        {
            builder.Append(quote);
        }

        for (Int32 i = 0; i < value.Length; i++)
        {
            Char c = value[i];
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Surrogate)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                if (category == UnicodeCategory.Surrogate)
                {
                    // an unpaired surrogate
                    builder.Append("\\u" + ((int)c).ToString("x4"));
                }
                else if (NeedsEscaping(category))
                {
                    // a surrogate pair that needs to be escaped
                    var unicode = char.ConvertToUtf32(value, i);
                    builder.Append("\\U" + unicode.ToString("x8"));
                    i++; // skip the already-encoded second surrogate of the pair
                }
                else
                {
                    // copy a printable surrogate pair directly
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
        {
            builder.Append(quote);
        }

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
            replaceWith = "\\u" + ((int)c).ToString("x4");
            return true;
        }

        return false;
    }

    public static String TrimLineEndWhitespace(this String input)
    {
        return Regex.Replace(input, @"[ \t]+(?=\r?$)", String.Empty, RegexOptions.Multiline);
    }
}
