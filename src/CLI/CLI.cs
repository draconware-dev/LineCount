using System.CommandLine;
using LineCount.Logging;

namespace LineCount.CLI;

public sealed class LinecountCommand : RootCommand
{
    public Option<string> FilterOption { get; } = new Option<string>("-f", "--filter")
    {
        Description = "A glob-pattern for files to include.",
        HelpName = "pattern"
    };

    public Option<string> LineFilterOption { get; } = new Option<string>("-l", "--line-filter")
    {
        Description = "A RegEx for the lines to count.",
        HelpName = "pattern"
    };

    public Option<string> ExceptFilterOption { get; } = new Option<string>("-x", "--exclude-filter")
    {
        Description = "A glob-pattern for files not to include.",
        HelpName = "pattern"
    };

    public Option<string> ExceptLineFilterOption { get; } = new Option<string>("-w", "--exlude-line-filter")
    {
        Description = "A RegEx for the lines not to count.",
        HelpName = "pattern"
    };

    public Option<bool> ListFilesOption { get; } = new Option<bool>("--list")
    {
        Description = "Whether to list the files as they are being processed."
    };

    public Option<Format> FormatOption { get; } = new Option<Format>("--format")
    {
        Description = "The output format of the result.",
        HelpName = "normal|raw|json"
    };

    public Option<string[]> ExcludeOption { get; } = new Option<string[]>("--exclude")
    {
        Description = "A list of files and directories to exclude.",
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true,
        HelpName = "paths"
    };


    public Argument<string> PathArgument { get; } = new Argument<string>("path")
    {
        Description = "The path to the file or the directory that contains the files to calculate the count of. Use '.' to refer to the current directory.", 
    };
    
    public LinecountCommand() : base("a tool to count the lines of projects")
    {
        FormatOption.CompletionSources.Add(Enum.GetValues<Format>().Select(value => value.ToString().ToLowerInvariant()).ToArray());

        Add(PathArgument);
        Add(FilterOption);
        Add(ExcludeOption);
        Add(LineFilterOption);
        Add(ExceptFilterOption);
        Add(ExceptLineFilterOption);
        Add(ListFilesOption);
        Add(FormatOption);

        Options.First(option => option.Name == "--version").Aliases.Add("-v");

        SetAction(Execute);
    }

    async Task Execute(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var filter = parseResult.GetValue(FilterOption);
        var lineFilter = parseResult.GetValue(LineFilterOption);
        var exceptFilter = parseResult.GetValue(ExceptFilterOption);
        var exceptLineFilter = parseResult.GetValue(ExceptLineFilterOption);
        var listFiles = parseResult.GetValue(ListFilesOption);
        var format = parseResult.GetValue(FormatOption);
        var excluded = parseResult.GetValue(ExcludeOption) ?? [];
        var path = parseResult.GetRequiredValue(PathArgument);

        LineCountData data = new LineCountData(filter, lineFilter, exceptFilter, exceptLineFilter)
        {
            ListFiles = listFiles
        };

        var (excludeFiles, excludeDirectories) = DetermineExclusions(excluded);

        var result = await LineCount.Run(path, data, excludeDirectories, excludeFiles, cancellationToken);

        if (listFiles)
        {
            Console.WriteLine();
        }

        result?.Match(
            report => Logger.LogReport(report, format),
            error => Logger.LogError(error)
            );
    }

    static (string[] excludeFiles, string[] excludeDirectories) DetermineExclusions(string[] excluded)
    {
        List<string> excludeFiles = new List<string>(excluded.Length);
        List<string> excludeDirectories = new List<string>(excluded.Length);

        foreach(string filePath in excluded)
        {
            if(Path.EndsInDirectorySeparator(filePath))
            {
                string directoryPath = Path.TrimEndingDirectorySeparator(filePath);
                excludeDirectories.Add(directoryPath);
                continue;
            }

            excludeFiles.Add(filePath);
        }


        return (excludeFiles.ToArray(), excludeDirectories.ToArray());
    }
}