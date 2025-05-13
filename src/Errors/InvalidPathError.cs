namespace LineCount.Errors;

public sealed record InvalidPathError(string Path) : Error
{
    public override string ToString()
    {
        var invalidChars = System.IO.Path.GetInvalidPathChars();
        return $"'{Path}' is invalid. {GetMessage(invalidChars)}";
    }

    static string GetMessage<T>(T[] characters)
    {
        switch (characters.Length)
        {
            case 1:
                return $"It must not contain '{characters[0]}'.";
            case 2:
                return $"It must contain neither '{characters[0]}' nor '{characters[1]}'.";
            default:
                string optionsText = string.Join(", ", characters[..^1].Select(x => $"'{x}'"));
                return $"It must not contain any of {optionsText} or '{characters[^1]}'.";
        }
    }
}