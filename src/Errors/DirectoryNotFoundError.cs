namespace Linecount.Errors;

public sealed record DirectoryNotFoundError(string Path) : Error
{
    public override string ToString()
    {
        return $"Directory '{Path}' was not found.";
    }
}