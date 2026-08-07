namespace Linecount;

public record struct FileLineCountReport(int Lines) : ILineCountReport
{
    public readonly int Files => Lines > 0 ? 1 : 0;
}