using SpanExtensions;

namespace LineCount;

public static class Globbing
{
    public static string ToRegex(string pattern)
    {
        if (!pattern.All(IsValidGlobPatternChar))
        {
            return pattern;
        }

        if (pattern.StartsWith('*') && pattern.EndsWith('*'))
        {
            return pattern[1..^1];
        }

        if (pattern.StartsWith('*'))
        {
            return $"{pattern[1..]}$";
        }

        if (pattern.EndsWith('*'))
        {
            return $"^{pattern[..^1]}";
        }

        return $"^{pattern}$";
    }

    static bool IsValidGlobPatternChar(char c)
    {
        return c == '*' || c == '_' || c == '-' || c == '.' || char.IsLetterOrDigit(c);
    }
}