namespace Linecount;

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

    public bool IsExcluded(string file)
    {
        ReadOnlySpan<char> fullPath = Path.GetFullPath(file);
        ReadOnlySpan<char> fullyTrimmedPath = Path.TrimEndingDirectorySeparator(fullPath);

        foreach(ReadOnlySpan<char> excludePath in ExcludeAbsolutePaths)
        {
            if(excludePath.SequenceEqual(fullyTrimmedPath))
            {
                return true;
            }
        }

        foreach(ReadOnlySpan<char> excludePath in ExcludeRelativePaths)
        {
            if(fullyTrimmedPath.EndsWith(excludePath))
            {
                return true;
            }
        }

        return false;
    }

    public static PathPatterns Create(IEnumerable<string> excludeFiles)
    {
        List<string> excludeFilePaths = [];
        List<string> excludeRelativeFilePaths = [];

        foreach(string filename in excludeFiles)
        {
            if(Path.IsPathFullyQualified(filename))
            {
                string currentPath = Path.TrimEndingDirectorySeparator(filename);
                excludeFilePaths.Add(currentPath);
                continue;
            }

            if(filename.StartsWith("./"))
            {
                string excludePath = CreateAbsoluteExclusion(filename[2..]);
                excludeFilePaths.Add(excludePath);
                continue;
            }

            if(filename.StartsWith(Path.DirectorySeparatorChar) || filename.StartsWith(Path.AltDirectorySeparatorChar))
            {
                string excludePath = CreateAbsoluteExclusion(filename[1..]);
                excludeFilePaths.Add(excludePath);
                continue;
            }

            excludeRelativeFilePaths.Add(filename);
        }

        return (excludeFilePaths.ToArray(), excludeRelativeFilePaths.ToArray());
    }

    static string CreateAbsoluteExclusion(string filename)
    {
        string currentPath = Path.TrimEndingDirectorySeparator(filename);
        string currentPathEnding = currentPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        string excludePath = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}{currentPathEnding}";
        return excludePath;
    }
}