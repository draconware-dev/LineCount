namespace LineCount;

public record LineCountReport(int Lines, int Files)
{
    public static LineCountReport FromLines(int lines)
    {
        return new LineCountReport(lines, 1);
    }

    public static implicit operator (int lines, int files)(LineCountReport value)
    {
        return (value.Lines, value.Files);
    }

    public static implicit operator LineCountReport((int lines, int files) value)
    {
        return new LineCountReport(value.lines, value.files);
    }

    public static LineCountReport operator +(LineCountReport left, LineCountReport right)
    {
        return (left.Lines + right.Lines, left.Files + right.Files);
    }
}