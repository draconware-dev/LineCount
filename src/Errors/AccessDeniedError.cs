namespace LineCount.Errors;

public sealed record AccessDeniedError(string Path) : Error
{
    public override string ToString()
    {
        return $"Access to '{Path}' has been denied. Attempt to re-run this program with elevated or administrative privileges.";
    }
}