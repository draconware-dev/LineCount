﻿using LineCount.Errors;
using LineCount.Logging;
using LineCount.Result;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace LineCount;

using ReportResult = Result<LineCountReport, IError>;

// The excessive exception handling is necessitated by the fact that thrown exceptions don't carry any information about the file that caused them, rendering top-level exception handling infeasible.
public static class LineCount
{
    public static async Task<ReportResult?> Run(string path, LineCountData data, string[] excludeDirectories, string[] excludeFiles, CancellationToken cancellationToken = default)
    {
        try
        {
            path = Path.TrimEndingDirectorySeparator(path);

            var excludeFilePatterns = PathPatterns.Create(path, excludeFiles);
            var excludeDirectoryPatterns = PathPatterns.Create(path, excludeDirectories);

            return await GetLineCount(path, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
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
                return filesReportResult;
            }
            
            var directoriesReportResult = await CountInDirectories(path, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken).ConfigureAwait(false);

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
        catch (IOException exception)
        {
            return new UndiagnosedError(exception);
        }
    }

    static async Task<ReportResult> CountInDirectories(string path, LineCountData data, PathPatterns excludeFilePatterns, PathPatterns excludeDirectoryPatterns, CancellationToken cancellationToken = default)
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

                var task = GetLineCount(directory, data, excludeFilePatterns, excludeDirectoryPatterns, cancellationToken);
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
        catch (IOException exception)
        {
            return new UndiagnosedError(exception);
        }

        int lineCount = 0;
        int fileCount = 0;
        
        await foreach (var result in Task.WhenEach(directorytasks))
        {
            if (!result.IsCompletedSuccessfully)
            {
                return HandleTaskFailure(result);
            }

            if (!result.Result.TryGetValue(out LineCountReport? report))
            {
                return ReportResult.Failure(result.Result.Error);
            }

            int lines = report.Lines;
            int files = report.Files;

            lineCount += lines;
            fileCount += files;
        }

        return new LineCountReport(lineCount, fileCount);
    }

    static ReportResult HandleTaskFailure<T>(Task<T> result)
    {
        if (result.IsCanceled)
        {
            throw new OperationCanceledException();
        }

        if (result.IsFaulted)
        {
            return new UndiagnosedError(result.Exception);
        }

        return new InternalError("Task has not been cancelled or faulted nor completed successfully");
    }

    static async Task<ReportResult> CountInFiles(string path, LineCountData data, PathPatterns excludeFilePatterns, CancellationToken cancellationToken = default)
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

                Task<Result<FileStats, IError>> task = GetSingleFileLineCount(file, data, cancellationToken)
                    .ContinueWith(task => task.Result.Map(
                        report => new FileStats(file, report.Lines)), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
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
        catch (IOException exception)
        {
            return new UndiagnosedError(exception);
        }

        int lineCount = 0;
        int fileCount = 0; 

        await foreach (var result in Task.WhenEach(filetasks))
        {
            if (!result.IsCompletedSuccessfully)
            {
                return HandleTaskFailure(result);
            }

            if (!result.Result.TryGetValue(out FileStats? fileStats))
            {
                return ReportResult.Failure(result.Result.Error);
            }

            int lines = fileStats.Lines;
            string file = fileStats.Path;

            if (data.ListFiles && lines > 0)
            {
                Logger.Log(file, lines.ToString(CultureInfo.InvariantCulture));
            }

            lineCount += lines;
            
            if(lines > 0)
            {
                fileCount++;
            }
        }

        return new LineCountReport(lineCount, fileCount);
    }

    static IEnumerable<string> GetFilterFilePaths(string path, LineCountData data)
    {
        var files = Directory.EnumerateFiles(path);

        if (data.Filter is not null)
        {
            files = files.Where(line => data.Filter.IsMatch(Path.GetFileName(line)));
        }

        if (data.ExcludeFilter is not null)
        {
            files = files.Where(line => !data.ExcludeFilter.IsMatch(Path.GetFileName(line)));
        }

        return files.Select(Path.GetFullPath);
    }

    static Task<LineCountReport> GetSingleFileLineCountReport(string path, LineCountData data, CancellationToken cancellationToken = default)
    {
        return (data.FilterType switch
        {
            FilterType.None => GetFileLineCount(path, cancellationToken),
            FilterType.Filtered => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line), cancellationToken),
            FilterType.FilteredExcept => GetFilteredFileLineCount(path, line => !data.ExcludeLineFilter!.IsMatch(line), cancellationToken),
            FilterType.FilteredBoth => GetFilteredFileLineCount(path, line => data.LineFilter!.IsMatch(line) && !data.ExcludeLineFilter!.IsMatch(line), cancellationToken),
            _ => throw new InvalidOperationException($"CountType.{data.FilterType} not recognized"),
        }).ContinueWith(task => LineCountReport.FromLines(task.Result), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
    }

    static async Task<ReportResult> GetSingleFileLineCount(string path, LineCountData data, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetSingleFileLineCountReport(path, data, cancellationToken).ConfigureAwait(false);
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
            return new InternalError(exception.Message);
        }
        catch(InvalidOperationException exception)
        {
            return new InternalError(exception.Message);
        }
        catch (RegexMatchTimeoutException)
        {
            return new BadInputError(BadInputError.Cause.RegexTimeOut);
        }
        catch(ArgumentOutOfRangeException)
        {
            return new BadInputError(BadInputError.Cause.LineLengthExceeded);
        }
        catch (IOException exception)
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

        while (line is not null && !cancellationToken.IsCancellationRequested)
        {
            if (filter(line))
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

        while (line is not null && !cancellationToken.IsCancellationRequested)
        {
            count++;
            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return count;
    }
}