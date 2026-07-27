namespace Linecount.Errors;

public sealed record DirectoryNotFoundError(string Path) : IError
{
    public override string ToString()
    {
        return $"Directory '{Path}' was not found.";
    }
}