namespace Linecount.Errors;

public sealed record FileNotFoundError(string Path) : IError
{
    public override string ToString()
    {
        return $"File or Directory '{Path}' was not found.";
    }
}