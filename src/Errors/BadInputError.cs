namespace LineCount.Errors;

public readonly struct BadInputError(BadInputError.Cause Reason) : IError
{
    public override string ToString()
    {
        return $"The Input appears to have been malformed due to {Reason}.";
    }

    public enum Cause
    {
        RegexTimeOut,
        LineLengthExceeded
    }
}