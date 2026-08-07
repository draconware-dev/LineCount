using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using Linecount.Errors;
using Linecount.Logging;
using Linecount.Result;

using ReportResult = Linecount.Result.Result<Linecount.ILineCountReport, Linecount.Errors.IError>;
using DirectoryReportResult = Linecount.Result.Result<Linecount.DirectoryLineCountReport, Linecount.Errors.IError>;

namespace Linecount;

// The excessive exception handling is necessitated by the fact that thrown exceptions don't carry any information about the file that caused them, rendering top-level exception handling infeasible.
public static class Loc
{
    public static async Task<ReportResult?> Run(string path, LineCountData data, string[] excludeDirectories, string[] excludeFiles, CancellationToken cancellationToken = default)
    {
        try
        {
            path = Path.TrimEndingDirectorySeparator(path);

            var excludeFilePatterns = PathPatterns.Create(excludeFiles);
            var excludeDirectoryPatterns = PathPatterns.Create(excludeDirectories);

            return await GetLineCount(path, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException)
        {
            return null;
        }
    }

    static async Task<ReportResult> GetLineCount(string path, LineCountData data, PathPatterns excludeFilePatterns, PathPatterns excludeDirectoryPatterns, CancellationToken cancellationToken = default)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);

            if(!attributes.HasFlag(FileAttributes.Directory))
            {
                return await GetSingleFileLineCountReport(path, data, cancellationToken).ConfigureAwait(false);
            }

            var filesReportResult = await CountInFiles(path, data, excludeFilePatterns, cancellationToken).ConfigureAwait(false);

            if(!filesReportResult.TryGetValue(out var filesReport))
            {
                return filesReportResult.Map<ILineCountReport>(r => r);
            }

            var directoriesReportResult = await CountInDirectories(path, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken).ConfigureAwait(false);

            if(!directoriesReportResult.TryGetValue(out var directoriesReport))
            {
                return directoriesReportResult.Map<ILineCountReport>(r => r);
            }

            return filesReport + directoriesReport;
        }
        catch(FileNotFoundException)
        {
            return new FileNotFoundError(path);
        }
        catch(DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch(UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch(PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch(NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch(IOException exception)
        {
            return new UndiagnosedError(exception);
        }
    }

    static async Task<DirectoryReportResult> CountInDirectories(string path, LineCountData data, PathPatterns excludeFilePatterns, PathPatterns excludeDirectoryPatterns, CancellationToken cancellationToken = default)
    {
        List<Task<ReportResult>> directorytasks = [];

        try
        {
            foreach(var directory in Directory.EnumerateDirectories(path))
            {
                if(excludeDirectoryPatterns.IsExcluded(directory))
                {
                    continue;
                }

                var task = GetLineCount(directory, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken);
                directorytasks.Add(task);
            }
        }
        catch(DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch(UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch(SecurityException)
        {
            return new AccessDeniedError(path);
        }
        catch(PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch(NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch(IOException exception)
        {
            return new UndiagnosedError(exception);
        }

        int lineCount = 0;
        int fileCount = 0;

        await foreach(var result in Task.WhenEach(directorytasks))
        {
            if(!result.IsCompletedSuccessfully)
            {
                return DirectoryReportResult.Failure(HandleTaskFailure(result));
            }

            if(!result.Result.TryGetValue(out ILineCountReport? report))
            {
                return DirectoryReportResult.Failure(result.Result.Error);
            }

            int lines = report.Lines;
            int files = report.Files;

            lineCount += lines;
            fileCount += files;
        }

        return new DirectoryLineCountReport(lineCount, fileCount, data.ListFiles);
    }

    static IError HandleTaskFailure<T>(Task<T> result)
    {
        if(result.IsCanceled)
        {
            throw new OperationCanceledException();
        }

        if(result.IsFaulted)
        {
            return new UndiagnosedError(result.Exception);
        }

        return new InternalError("Task has not been cancelled or faulted nor completed successfully");
    }

    static async Task<DirectoryReportResult> CountInFiles(string path, LineCountData data, PathPatterns excludeFilePatterns, CancellationToken cancellationToken = default)
    {
        List<Task<Result<FileStats, IError>>> filetasks = [];
        try
        {
            var allFiles = Directory.EnumerateFiles(path);
            IEnumerable<string> files = GetFilterFilePaths(allFiles, data);

            foreach(var file in files)
            {
                if(excludeFilePatterns.IsExcluded(file))
                {
                    continue;
                }

                Task<Result<FileStats, IError>> task = GetSingleFileLineCount(file, data, cancellationToken)
                    .ContinueWith(task => task.Result.Map(
                        report => new FileStats(file, report.Lines)), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
                filetasks.Add(task);
            }
        }
        catch(DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch(UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch(SecurityException)
        {
            return new AccessDeniedError(path);
        }
        catch(PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch(NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch(IOException exception)
        {
            return new UndiagnosedError(exception);
        }

        int lineCount = 0;
        int fileCount = 0;

        await foreach(var result in Task.WhenEach(filetasks))
        {
            if(!result.IsCompletedSuccessfully)
            {
                return DirectoryReportResult.Failure(HandleTaskFailure(result));
            }

            if(!result.Result.TryGetValue(out FileStats? fileStats))
            {
                return DirectoryReportResult.Failure(result.Result.Error);
            }

            int lines = fileStats.Lines;
            string file = fileStats.Path;

            if(data.ListFiles && lines > 0)
            {
                Logger.Log(file, lines.ToString(CultureInfo.InvariantCulture));
            }

            lineCount += lines;

            if(lines > 0)
            {
                fileCount++;
            }
        }

        return new DirectoryLineCountReport(lineCount, fileCount, data.ListFiles);
    }

    static IEnumerable<string> GetFilterFilePaths(IEnumerable<string> files, LineCountData data)
    {
        if(data.Filter is not null)
        {
            files = files.Where(line => data.Filter.IsMatch(Path.GetFileName(line)));
        }

        if(data.ExcludeFilter is not null)
        {
            files = files.Where(line => !data.ExcludeFilter.IsMatch(Path.GetFileName(line)));
        }

        return files.Select(Path.GetFullPath);
    }

    static Task<FileLineCountReport> GetSingleFileLineCountReport(string path, LineCountData data, CancellationToken cancellationToken = default)
    {
        return (data.FilterType switch
        {
            FilterType.None => GetFileLineCount(path, cancellationToken),
            FilterType.Filtered => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line), cancellationToken),
            FilterType.FilteredExcept => GetFilteredFileLineCount(path, line => !data.ExcludeLineFilter!.IsMatch(line), cancellationToken),
            FilterType.FilteredBoth => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line) && !data.ExcludeLineFilter!.IsMatch(line), cancellationToken),
            _ => throw new InvalidOperationException($"CountType.{data.FilterType} not recognized"),
        }).ContinueWith(task => new FileLineCountReport(task.Result), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
    }

    static async Task<ReportResult> GetSingleFileLineCount(string path, LineCountData data, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetSingleFileLineCountReport(path, data, cancellationToken).ConfigureAwait(false);
        }
        catch(FileNotFoundException)
        {
            return new FileNotFoundError(path);
        }
        catch(DirectoryNotFoundException)
        {
            return new DirectoryNotFoundError(path);
        }
        catch(UnauthorizedAccessException)
        {
            return new AccessDeniedError(path);
        }
        catch(PathTooLongException)
        {
            return new InvalidPathError(path);
        }
        catch(NotSupportedException)
        {
            return new InvalidPathError(path);
        }
        catch(ObjectDisposedException exception)
        {
            return new InternalError(exception.Message);
        }
        catch(InvalidOperationException exception)
        {
            return new InternalError(exception.Message);
        }
        catch(RegexMatchTimeoutException)
        {
            return new BadInputError(BadInputError.Cause.RegexTimeOut);
        }
        catch(ArgumentOutOfRangeException)
        {
            return new BadInputError(BadInputError.Cause.LineLengthExceeded);
        }
        catch(IOException exception)
        {
            return new UndiagnosedError(exception);
        }
    }

    public static async Task<int> GetFilteredFileLineCount(string path, Predicate<string> filter, CancellationToken cancellationToken = default)
    {
        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        int count = 0;

        while(line is not null && !cancellationToken.IsCancellationRequested)
        {
            if(filter(line))
            {
                count++;
            }

            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return count;
    }

    public static async Task<int> GetFileLineCount(string path, CancellationToken cancellationToken = default)
    {
        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        int count = 0;

        while(line is not null && !cancellationToken.IsCancellationRequested)
        {
            count++;
            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return count;
    }
}