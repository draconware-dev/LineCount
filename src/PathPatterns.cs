namespace LineCount;

public record PathPatterns(string[] ExcludeNames, string[] ExcludeAbsolutePaths, string[] ExcludeRelativePaths)
{
    public static implicit operator (string[] excludeNames, string[] excludePaths, string[] excludeRelativePaths)(PathPatterns value)
    {
        return (value.ExcludeNames, value.ExcludeAbsolutePaths, value.ExcludeRelativePaths);
    }

    public static implicit operator PathPatterns((string[] excludeNames, string[] excludePaths, string[] excludeRelativePaths) value)
    {
        return new PathPatterns(value.excludeNames, value.excludePaths, value.excludeRelativePaths);
    }
}