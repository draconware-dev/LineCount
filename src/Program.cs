using System.CommandLine;
using System.CommandLine.Invocation;
using LineCount;
using LineCount.Logging;

var rootCommand = new RootCommand("linecount");

var filterOption = new Option<string>(["-f", "--filter"], "A glob-pattern for files to include.");
var lineFilterOption = new Option<string>(["-l", "--line-filter"], "A RegEx for the lines to count.");
var exceptFilterOption = new Option<string>(["-x", "--exclude-filter"], "A glob-pattern for files not to include.");
var exceptLineFilterOption = new Option<string>(["-w", "--exlude-line-filter"], "A RegEx for the lines not to count.");
var listFilesOption = new Option<bool>("--list", "Whether to list the files as they are being processed.");
var formatOption = new Option<Format>("--format", "The output format of the result.");
var excludeDirectoriesOption = new Option<string[]>("--exclude-directories", "A list of directories to exclude.")
{
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true
};
var excludeFilesOption = new Option<string[]>("--exclude-files", "A list of files to exclude.")
{
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true
};

var pathArgument = new Argument<string>("path", "The path to the file or the directory that contains the files to calculate the count of. Use '.' to refer to the current directory.");

rootCommand.AddArgument(pathArgument);
rootCommand.AddOption(filterOption);
rootCommand.AddOption(excludeDirectoriesOption);
rootCommand.AddOption(excludeFilesOption);
rootCommand.AddOption(lineFilterOption);
rootCommand.AddOption(exceptFilterOption);
rootCommand.AddOption(exceptLineFilterOption);
rootCommand.AddOption(listFilesOption);
rootCommand.AddOption(formatOption);

rootCommand.SetHandler(async (InvocationContext context) =>
{
    var filter = context.ParseResult.GetValueForOption(filterOption);
    var lineFilter = context.ParseResult.GetValueForOption(lineFilterOption);
    var exceptFilter = context.ParseResult.GetValueForOption(exceptFilterOption);
    var exceptLineFilter = context.ParseResult.GetValueForOption(exceptLineFilterOption);
    var listFiles = context.ParseResult.GetValueForOption(listFilesOption);
    var format = context.ParseResult.GetValueForOption(formatOption);
    var excludeDirectories = context.ParseResult.GetValueForOption(excludeDirectoriesOption);
    var excludeFiles = context.ParseResult.GetValueForOption(excludeFilesOption);
    var path = context.ParseResult.GetValueForArgument(pathArgument);

    LineCountData data = new LineCountData(filter, lineFilter, exceptFilter, exceptLineFilter)
    {
        ListFiles = listFiles,
        Format = format
    };
    
    var result = await LineCount.LineCount.Run(path, data, excludeDirectories ?? [], excludeFiles ?? []);
    
    if(listFiles)
    {
        Console.WriteLine();
    }
    
    result.Match(
        report => Logger.LogReport(report),
        error => Logger.LogError(error)
        );
});

return await rootCommand.InvokeAsync(args);