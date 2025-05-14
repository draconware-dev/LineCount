namespace LineCount.Errors;

public sealed record InternalError(string Message) : ReportError(Message)
{
    protected override string Name => "Internal Error";
}