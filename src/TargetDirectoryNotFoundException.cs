namespace LineCount;

public class TargetDirectoryNotFoundException : Exception
{
    public TargetDirectoryNotFoundException()
    {
    }

    public TargetDirectoryNotFoundException(string? message) : base(message)
    {
    }

    public TargetDirectoryNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}