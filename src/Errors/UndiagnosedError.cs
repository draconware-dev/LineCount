namespace Linecount.Errors;

public sealed class UndiagnosedError(Exception exception) : ReportError(exception.Message)
{
    protected override string Name => "Something unexpectedly went wrong";
}