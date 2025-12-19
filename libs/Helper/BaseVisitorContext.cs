namespace Parseidon.Helper;

public class BaseVisitorContext
{
    public BaseVisitorContext(String text)
    {
        Text = text;
    }

    private String Text { get; }
    public (UInt32, UInt32) CalcLocation(Int32 position)
    {
        Int32 row = 1;
        Int32 column = 1;
        Int32 limit = position;
        if (limit > Text.Length)
            limit = Text.Length;

        for (Int32 index = 0; index < limit; index++)
        {
            if (Text[index] == '\n')
            {
                row++;
                column = 1;
            }
            else
            {
                column++;
            }
        }
        return ((UInt32)row, (UInt32)column);
    }

}