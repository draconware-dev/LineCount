namespace LineCount.Errors;

public sealed record InternalError(Exception Exception) : Error
{
    public override string ToString()
    {
        return $"""
            {Exception}
            """;
    }
}