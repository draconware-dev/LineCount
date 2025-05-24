namespace LineCount;

public record LineCountReport(int Lines, int Files)
{
    public static LineCountReport operator +(LineCountReport left, LineCountReport right)
    {
        return new LineCountReport(left.Lines + right.Lines, left.Files + right.Files);
    }

    public static LineCountReport FromLines(int lines)
    {
        return new LineCountReport(lines, lines > 0 ? 1 : 0);
    }
}