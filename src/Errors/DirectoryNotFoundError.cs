namespace LineCount.Errors;

public sealed record DirectoryNotFoundError(string Path) : Error;