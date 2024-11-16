namespace LineCount;

public sealed record PathPatterns(string[] ExcludeAbsolutePaths, string[] ExcludeRelativePaths)
{
    public static implicit operator (string[] excludePaths, string[] excludeRelativePaths)(PathPatterns value)
    {
        return (value.ExcludeAbsolutePaths, value.ExcludeRelativePaths);
    }

    public static implicit operator PathPatterns((string[] excludePaths, string[] excludeRelativePaths) value)
    {
        return new PathPatterns(value.excludePaths, value.excludeRelativePaths);
    }
}