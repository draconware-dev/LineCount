namespace LineCount;

public record LineCountReport(int Lines, int Files = 1)
{
    public static LineCountReport operator +(LineCountReport left, LineCountReport right)
    {
        return new LineCountReport(left.Lines + right.Lines, left.Files + right.Files);
    }
}