namespace Linecount;

public record struct DirectoryLineCountReport(int Lines, int Files, bool ListFiles) : ILineCountReport
{
    public static DirectoryLineCountReport operator +(DirectoryLineCountReport left, DirectoryLineCountReport right)
    {
        return new DirectoryLineCountReport(left.Lines + right.Lines, left.Files + right.Files, left.ListFiles || right.ListFiles);
    }
}