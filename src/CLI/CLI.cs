using System.CommandLine;
using System.CommandLine.Invocation;
using LineCount.Logging;

namespace LineCount.CLI;

public sealed class LinecountCommand : RootCommand
{
    public Option<string> filterOption { get; } = new Option<string>(["-f", "--filter"], "A glob-pattern for files to include.")
    {
        ArgumentHelpName = "pattern"
    };

    public Option<string> lineFilterOption { get; } = new Option<string>(["-l", "--line-filter"], "A RegEx for the lines to count.")
    {
        ArgumentHelpName = "pattern"
    };

    public Option<string> exceptFilterOption { get; } = new Option<string>(["-x", "--exclude-filter"], "A glob-pattern for files not to include.")
    {
        ArgumentHelpName = "pattern"
    };

    public Option<string> exceptLineFilterOption { get; } = new Option<string>(["-w", "--exlude-line-filter"], "A RegEx for the lines not to count.")
    {
        ArgumentHelpName = "pattern"
    };

    public Option<bool> listFilesOption { get; } = new Option<bool>("--list", "Whether to list the files as they are being processed.");

    public Option<Format> formatOption { get; } = new Option<Format>("--format", "The output format of the result.")
    {
        ArgumentHelpName = "normal|raw|json"
    };

    public Option<string[]> excludeOption { get; } = new Option<string[]>("--exclude", "A list of files and directories to exclude.")
    {
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true,
        ArgumentHelpName = "paths"
    };


    public Argument<string> pathArgument =
        new Argument<string>("path", "The path to the file or the directory that contains the files to calculate the count of. Use '.' to refer to the current directory.");

    public LinecountCommand() : base("a tool to count the lines of projects")
    {
        Name = "linecount";

        formatOption.AddCompletions(Enum.GetValues<Format>().Select(value => value.ToString().ToLowerInvariant()).ToArray());

        AddArgument(pathArgument);
        AddOption(filterOption);
        AddOption(excludeOption);
        AddOption(lineFilterOption);
        AddOption(exceptFilterOption);
        AddOption(exceptLineFilterOption);
        AddOption(listFilesOption);
        AddOption(formatOption);

        this.SetHandler(Execute);
    }

    async Task Execute(InvocationContext context)
    {
        var filter = context.ParseResult.GetValueForOption(filterOption);
        var lineFilter = context.ParseResult.GetValueForOption(lineFilterOption);
        var exceptFilter = context.ParseResult.GetValueForOption(exceptFilterOption);
        var exceptLineFilter = context.ParseResult.GetValueForOption(exceptLineFilterOption);
        var listFiles = context.ParseResult.GetValueForOption(listFilesOption);
        var format = context.ParseResult.GetValueForOption(formatOption);
        var excluded = context.ParseResult.GetValueForOption(excludeOption) ?? [];
        var path = context.ParseResult.GetValueForArgument(pathArgument);

        LineCountData data = new LineCountData(filter, lineFilter, exceptFilter, exceptLineFilter)
        {
            ListFiles = listFiles
        };

        var (excludeFiles, excludeDirectories) = DetermineExclusions(excluded);

        var result = await LineCount.Run(path, data, excludeDirectories, excludeFiles, context.GetCancellationToken());

        if(listFiles)
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