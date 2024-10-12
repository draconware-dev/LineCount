using System.Text.RegularExpressions;
using LineCount.Errors;

namespace LineCount;

public static partial class LineCount
{
    public static async Task<Result<int, DirectoryNotFoundError>> GetLineCount(LineCountData data,  string[] excludeDirectories, string[] excludeFiles)
    {
        var (excludeFileNames, excludeFilePaths, excludeRelativeFilePaths) = ExcludePaths(excludeFiles);
        var (excludeDirectoryNames, excludeDirectoryPaths, excludeRelativeDirectoryPaths) = ExcludePaths(excludeDirectories);

        if (File.Exists(data.Path))
        {
            return await GetSingleLineCount(data);
        }

        if (!Directory.Exists(data.Path))
        {
            return new DirectoryNotFoundError(data.Path);
        }

        excludeFiles ??= [];
        excludeDirectories ??= [];

        List<Task<int>> filetasks = [];
        foreach (var file in data.Filter is not null ? Directory.GetFiles(data.Path, data.Filter) : Directory.GetFiles(data.Path))
        {
            if (!Array.Exists(excludeFileNames, x => x == Path.GetFileName(file))
                && !Array.Exists(excludeFilePaths, x => x == Path.GetFullPath(file)
                && !excludeRelativeFilePaths.Any(x => Path.GetFullPath(file).Contains(x))))
            {
                switch(data.FilterType)
                {
                    case CountType.Normal:
                        filetasks.Add(GetFileLineCount(file));
                        break;
                    case CountType.Filtered:
                        filetasks.Add(GetFilteredFileLineCount(file, data.LineFilter!));
                        break;
                    case CountType.FilteredExcept:
                        filetasks.Add(GetFilteredFileLineCount(file, data.LineFilterNot!, false));
                        break;
                    case CountType.FilteredBoth:
                        filetasks.Add(GetDoublyFilteredFileLineCount(file, data.LineFilter!, data.LineFilterNot!));
                        break;
                }
            }
        }

        var filetaskResults = await Task.WhenAll(filetasks);
        int rootLineCount = filetaskResults.Sum();

        List<Task<Result<int, DirectoryNotFoundError>>> directorytasks = [];

        foreach (var directory in Directory.GetDirectories(data.Path))
        {
            if (!Array.Exists(excludeDirectories, x => x == Path.GetFileName(directory)))
            {
                directorytasks.Add(GetLineCount(data, excludeDirectories, excludeFiles));
            }
        }

        var directorytasksResult = await Task.WhenAll(directorytasks);
        int directoriescount = directorytasksResult.Where(x => x.IsSuccess).Sum(x => x.Value);

        return rootLineCount + directoriescount;
    }

    private static async Task<Result<int, DirectoryNotFoundError>> GetSingleLineCount(LineCountData data)
    {
        return await (data.FilterType switch
        {
            CountType.Normal => GetFileLineCount(data.Path),
            CountType.Filtered => GetFilteredFileLineCount(data.Path, data.LineFilter!),
            CountType.FilteredExcept => GetFilteredFileLineCount(data.Path, data.LineFilterNot!, false),
            CountType.FilteredBoth => GetDoublyFilteredFileLineCount(data.Path, data.LineFilter!, data.LineFilterNot!),
            _ => throw new NotImplementedException(),
        });
    }

    public static async Task<int> GetFilteredFileLineCount(string path, Regex regex, bool filterResult = true)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;
        while (line is not null)
        {
            if (regex.IsMatch(line) == filterResult)
            {
                count++;
            }

            line = await reader.ReadLineAsync();
        }
        return count;
    }

    public static async Task<int> GetDoublyFilteredFileLineCount(string path, Regex regex, Regex regexNot)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;
        while (line is not null)
        {
            if (regex.IsMatch(line) && !regex.IsMatch(line))
            {
                count++;
            }

            line = await reader.ReadLineAsync();
        }
        return count;
    }

    public static async Task<int> GetFileLineCount(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;

        while (line is not null)
        {
            count++;
            line = await reader.ReadLineAsync();
        }

        return count;
    }

    static PathPatterns ExcludePaths(string[] excludeFiles)
    {
        List<string> excludeFileNames = new List<string>();
        List<string> excludeFilePaths = new List<string>();
        List<string> excludeRelativeFilePaths = new List<string>();

        foreach (string filename in excludeFiles)
        {
            if (!filename.Contains(Path.DirectorySeparatorChar) && !filename.Contains(Path.AltDirectorySeparatorChar))
            {
                excludeFileNames.Add(filename);
                continue;
            }

            if (Path.IsPathFullyQualified(filename))
            {
                excludeFilePaths.Add(filename);
                continue;
            }

            if (filename.StartsWith("./"))
            {
                excludeRelativeFilePaths.Add(filename[2..]);
            }
        }
        return (excludeFileNames.ToArray(), excludeFilePaths.ToArray(), excludeRelativeFilePaths.ToArray());
    }
}