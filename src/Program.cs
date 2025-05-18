using System.CommandLine;
using LineCount;
using LineCount.Logging;

var rootCommand = new RootCommand("linecount");

var filterOption = new Option<string>(["-f", "--filter"], "A glob-pattern for files to include.");
var lineFilterOption = new Option<string>(["-l", "--line-filter"], "A RegEx for the lines to count.");
var exceptFilterOption = new Option<string>(["-x", "--exclude-filter"], "A glob-pattern for files not to include.");
var exceptLineFilterOption = new Option<string>(["-w", "--exlude-line-filter"], "A RegEx for the lines not to count.");
var listFilesOption = new Option<bool>("--list", "Whether to list the files as they are being processed.");
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

rootCommand.SetHandler(async (path, filter, lineFilter, exceptFilter, exceptLineFilter, excludeDirectories, excludeFilesOption, listFiles) =>
{   
    LineCountData data = new LineCountData(filter, lineFilter, exceptFilter, exceptLineFilter, listFiles);    
    var result = await LineCount.LineCount.Run(path, data, excludeDirectories, excludeFilesOption);
    
    if(listFiles)
    {
        Console.WriteLine();
    }
    
    result.Match(
        report => Logger.LogReport(report),
        error => Logger.LogError(error)
        );
}, pathArgument, filterOption, lineFilterOption, exceptFilterOption, exceptLineFilterOption, excludeDirectoriesOption, excludeFilesOption, listFilesOption);

return await rootCommand.InvokeAsync(args);