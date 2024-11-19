using System.IO;
using System.Text.RegularExpressions;
using LineCount.Errors;
using SpanExtensions;

namespace LineCount;

public static partial class LineCount
{
    public static async Task<Result<string, DirectoryNotFoundError>> GetLineCount(string path, LineCountData data, string[] excludeDirectories, string[] excludeFiles)
    {
        var (excludeFilePaths, excludeRelativeFilePaths) = ExcludePaths(excludeFiles);
        var (excludeDirectoryPaths, excludeRelativeDirectoryPaths) = ExcludePaths(excludeDirectories);

        string[] excludeAbsoluteFilePaths = CombinePaths(excludeFilePaths, excludeRelativeFilePaths);
        string[] excludeAbsoluteDirectoryPaths = CombinePaths(excludeDirectoryPaths, excludeRelativeDirectoryPaths);

        var result = await GetLineCountFromAbsolutePaths(path, data, excludeAbsoluteFilePaths, excludeAbsoluteDirectoryPaths);

        return result.Map(x => $"{x} lines have been found.");
    }

    static async Task<Result<int, DirectoryNotFoundError>> GetLineCountFromAbsolutePaths(string path, LineCountData data, string[] excludeFiles, string[] excludeDirectories)
    {
        if (File.Exists(path))
        {
            return await GetSingleLineCount(path, data);
        }

        if (!Directory.Exists(path))
        {
            return new DirectoryNotFoundError(path);
        }

        List<Task<int>> filetasks = [];
        
        string[] files = (data.Filter is not null ?
            Directory.GetFiles(path, data.Filter) :
            Directory.GetFiles(path)).Select(Path.GetFullPath).ToArray();
         
        foreach (var file in files)
        {
            if (IsExcluded(excludeFiles, file))
            {
                continue;
            }

            switch (data.FilterType)
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
        
        int rootLineCount = 0;
        int index = 0;
        
        await foreach (var result in Task.WhenEach(filetasks))
        {
            if(data.ListFiles && result.Result != 0)
            {
                Logger.Log(files[index], result.Result.ToString());
            }

            rootLineCount += result.Result;
            index++;
        }

        List<Task<Result<int, DirectoryNotFoundError>>> directorytasks = [];

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (!IsExcluded(excludeDirectories, directory))
            {
                directorytasks.Add(GetLineCountFromAbsolutePaths(directory, data, excludeDirectories, excludeFiles));
            }

        }
        
        var directorytasksResult = await Task.WhenAll(directorytasks);
        int directoriescount = directorytasksResult.Where(x => x.IsSuccess).Sum(x => x.Value);

        return rootLineCount + directoriescount;
    }

    static string[] CombinePaths(string[] excludeFilePaths, string[] excludeRelativeFilePaths)
    {
        string[] excludeAbsoluteFilePaths = new string[excludeFilePaths.Length + excludeRelativeFilePaths.Length];

        ConvertRelativePathsToAbsolute(excludeAbsoluteFilePaths, excludeRelativeFilePaths);

        Array.Copy(excludeFilePaths, 0, excludeAbsoluteFilePaths, excludeRelativeFilePaths.Length, excludeFilePaths.Length);
        return excludeAbsoluteFilePaths;
    }

    static void ConvertRelativePathsToAbsolute(string[] absolutePaths, string[] relativePaths)
    {
        ReadOnlySpan<char> workingDir = Environment.CurrentDirectory;

        if (workingDir[^1] == Path.DirectorySeparatorChar)
        {
            workingDir = workingDir[..^1];
        }

        for (int i = 0; i < relativePaths.Length; i++)
        {
            absolutePaths[i] = $"{workingDir}{Path.DirectorySeparatorChar}{relativePaths[i]}";
        }
    }
    static bool IsExcluded(string[] excludeFilePaths, string file)
    {
        ReadOnlySpan<char> fullPath = Path.GetFullPath(file);
        ReadOnlySpan<char> fullyTrimmedPath = Path.TrimEndingDirectorySeparator(fullPath);

        foreach(ReadOnlySpan<char> excludePath in excludeFilePaths)
        {
            if(excludePath.SequenceEqual(fullyTrimmedPath))
            {
                return true;
            }
        }
        return false;
    }

    static Task<int> GetSingleLineCount(string path, LineCountData data)
    {
        return data.FilterType switch
        {
            CountType.Normal => GetFileLineCount(path),
            CountType.Filtered => GetFilteredFileLineCount(path, data.LineFilter!),
            CountType.FilteredExcept => GetFilteredFileLineCount(path, data.LineFilterNot!, false),
            CountType.FilteredBoth => GetDoublyFilteredFileLineCount(path, data.LineFilter!, data.LineFilterNot!),
            _ => throw new NotImplementedException(),
        };
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

    static PathPatterns ExcludePaths(IEnumerable<string> excludeFiles)
    {
        List<string> excludeFilePaths = [];
        List<string> excludeRelativeFilePaths = [];

        foreach (string filename in excludeFiles)
        {
            if (Path.IsPathFullyQualified(filename))
            {
                excludeFilePaths.Add(filename);
                continue;
            }

            if (filename.StartsWith("./"))
            {
                excludeRelativeFilePaths.Add(filename[2..]);
                continue;
            }

            excludeRelativeFilePaths.Add(filename);
        }
        return (excludeFilePaths.ToArray(), excludeRelativeFilePaths.ToArray());
    }
}
