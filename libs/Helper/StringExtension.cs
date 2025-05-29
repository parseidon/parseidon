namespace Parseidon.Helper;

public static class StringExtensions
{
    public static string ReplaceAt(this string source, int index, int length, string replacement)
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
}
