namespace Linecount.Errors;

public sealed class InternalError(string message) : ReportError(message)
{
    protected override string Name => "Internal Error";
}