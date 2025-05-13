namespace LineCount.Errors;

public readonly struct BadInputError : IError
{
    public override string ToString()
    {
        return $"The Input appears to have been malformed.";
    }
}