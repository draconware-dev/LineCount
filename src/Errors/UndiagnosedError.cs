namespace LineCount.Errors;

public sealed record UndiagnosedError(Exception Exception) : ReportError(Exception.Message)
{
    protected override string Name => "Something unexpectedly went wrong";
}