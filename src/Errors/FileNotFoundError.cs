namespace LineCount.Errors;

public sealed record FileNotFoundError(string Path) : Error
{
    public override string ToString()
    {
        return $"File or Directory '{Path}' was not found.";
    }
}