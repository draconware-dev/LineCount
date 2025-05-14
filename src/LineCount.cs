using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using LineCount.Errors;

namespace LineCount;

using ReportResult = Result<LineCountReport, IError>;

// The excessive exception handling is necessitated by the fact that thrown exceptions don't carry any information about the file that caused them, rendering top-level exception handling infeasible.
public static class LineCount
{
    public static async Task<ReportResult> Run(string path, LineCountData data, string[] excludeDirectories, string[] excludeFiles)
    {
        path = Path.TrimEndingDirectorySeparator(path);
        
        var excludeFilePatterns = PathPatterns.Create(path, excludeFiles);
        var excludeDirectoryPatterns = PathPatterns.Create(path, excludeDirectories);
        
        return await GetLineCount(path, data, excludeFilePatterns, excludeDirectoryPatterns); 
     }

    static async Task<ReportResult> GetLineCount(string path, LineCountData data, PathPatterns excludeFilePatterns, PathPatterns excludeDirectoryPatterns)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);

            if(!attributes.HasFlag(FileAttributes.Directory))
            {
                return await GetSingleFileLineCount(path, data);
            }

            var filesReportResult = await CountInFiles(path, data, excludeFilePatterns);
            
            if(!filesReportResult.TryGetValue(out var filesReport))
            {
                return filesReportResult;
            }
            
            var directoriesReportResult = await CountInDirectories(path, data, excludeFilePatterns, excludeDirectoryPatterns);

            if (!directoriesReportResult.TryGetValue(out var directoriesReport))
            {
                return directoriesReportResult;
            }

            return filesReport + directoriesReport;
        }
        catch (FileNotFoundException)
        {
            return new FileNotFoundError(path);
        }
        catch (DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch (UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch(PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch (NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch (IOException)
        {
            return null;
        }
    }

    static async Task<ReportResult> CountInDirectories(string path, LineCountData data, PathPatterns excludeFilePatterns, PathPatterns excludeDirectoryPatterns)
    {
        List<Task<ReportResult>> directorytasks = [];

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                if (excludeDirectoryPatterns.IsExcluded(directory))
                {
                    continue;
                }

                var task = GetLineCount(directory, data, excludeFilePatterns, excludeDirectoryPatterns);
                directorytasks.Add(task);
            }
        }
        catch (DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch (UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch (SecurityException)
        {
            return new AccessDeniedError(path);
        }
        catch (PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch (NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch (IOException)
        {
            return null;
        }

        int lineCount = 0;
        int fileCount = 0;
        int index = 0;

        await foreach (var result in Task.WhenEach(directorytasks))
        {
            if (!result.Result.TryGetValue(out LineCountReport? report))
            {
                return ReportResult.Failure(result.Result.Error);
            }

            int lines = report.Lines;
            int files = report.Files;

            lineCount += lines;
            fileCount += files;
            index++;
        }

        return new LineCountReport(lineCount, fileCount);
    }

    static async Task<ReportResult> CountInFiles(string path, LineCountData data, PathPatterns excludeFilePatterns)
    {
        List<Task<Result<FileStats, IError>>> filetasks = [];

        try
        {
            IEnumerable<string> files = GetFilterFilePaths(path, data);

            foreach (var file in files)
            {
                if (excludeFilePatterns.IsExcluded(file))
                {
                    continue;
                }

                Task<Result<FileStats, IError>> task = GetSingleFileLineCount(file, data)
                    .ContinueWith(task => task.Result.Map(report => new FileStats(file, report.Lines)));
                filetasks.Add(task);
            }
        }
        catch (DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch (UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch (SecurityException)
        {
            return new AccessDeniedError(path);
        }
        catch (PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch (NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch (IOException)
        {
            return null;
        }

        int rootLineCount = 0;
        int index = 0;

        await foreach (var result in Task.WhenEach(filetasks))
        {
            if(!result.Result.TryGetValue(out FileStats? fileStats))
            {
                return ReportResult.Failure(result.Result.Error);
            }

            int lines = fileStats.Lines;
            string file = fileStats.Path;

            if (data.ListFiles && lines > 0)
            {
                Logger.Log(file, lines.ToString(CultureInfo.InvariantCulture));
            }

            rootLineCount += lines;
            index++;
        }

        return new LineCountReport(rootLineCount, index);
    }

    static IEnumerable<string> GetFilterFilePaths(string path, LineCountData data)
    {
        if (data.Filter is null)
        {
            return Directory.EnumerateFiles(path).Select(Path.GetFullPath);
        }
      
        return Directory.EnumerateFiles(path, data.Filter).Select(Path.GetFullPath);
    }

    static Task<LineCountReport> GetSingleFileLineCountReport(string path, LineCountData data)
    {
        return (data.FilterType switch
        {
            FilterType.None => GetFileLineCount(path),
            FilterType.Filtered => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line)),
            FilterType.FilteredExcept => GetFilteredFileLineCount(path, line => !data.LineFilterNot!.IsMatch(line)),
            FilterType.FilteredBoth => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line) && !data.LineFilterNot!.IsMatch(line)),
            _ => throw new InvalidOperationException($"CountType.{data.FilterType} not recognized"),
        }).ContinueWith(x => LineCountReport.FromLines(x.Result));
    }

    static async Task<ReportResult> GetSingleFileLineCount(string path, LineCountData data)
    {
        try
        {
            return await GetSingleFileLineCountReport(path, data);
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            return new FileNotFoundError(fileNotFoundException.FileName ?? "");
        }
        catch (DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch (UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch (PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch (NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch(ObjectDisposedException exception)
        {
            return new InternalError(exception);
        }
        catch(InvalidOperationException exception)
        {
            return new InternalError(exception);
        }
        catch (RegexMatchTimeoutException)
        {
            return new BadInputError();
        }
        catch(ArgumentOutOfRangeException)
        {
            return new BadInputError();
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static async Task<int> GetFilteredFileLineCount(string path, Predicate<string> filter)
    {
        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;

        while (line is not null)
        {
            if (filter(line))
            {
                count++;
            }

            line = await reader.ReadLineAsync();
        }

        return count;
    }

    public static async Task<int> GetFileLineCount(string path)
    {
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
}