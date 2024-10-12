using System.CommandLine;
using LineCount;

var rootCommand = new RootCommand("Line Count");

var filterOption = new Option<string>(["-f", "--filter"], "a glob-pattern for files to include.");
var lineFilterOption = new Option<string>(["-l", "--line-filter"], "a RegEx for the lines to count.");
var exceptFilterOption = new Option<string>(["-x", "--exclude-filter"], "a glob-pattern for files not to include.");
var exceptLineFilterOption = new Option<string>(["-w", "--exlude-line-filter"], "a RegEx for the lines not to count.");
var exludeDirectoriesOption = new Option<string[]>("--exclude-directories", "a list of directories to exlude.")
{
    Arity = ArgumentArity.ZeroOrMore
};
var exludeFilesOption = new Option<string[]>("--exclude-files", "a list of files to exlude.")
{
    Arity = ArgumentArity.ZeroOrMore
};

var pathArgument = new Argument<string>("path", "The path of the directory that contains the files to calculate the count of. Use '.' to refer to the current directory.");

rootCommand.AddArgument(pathArgument);
rootCommand.AddOption(filterOption);
rootCommand.AddOption(exludeDirectoriesOption);
rootCommand.AddOption(exludeFilesOption);
rootCommand.AddOption(lineFilterOption);
rootCommand.AddOption(exceptFilterOption);
rootCommand.AddOption(exceptLineFilterOption);

rootCommand.SetHandler(async (path, filter, lineFilter, exceptFilter, exceptLineFilter, exludeDirectories, exludeFilesOption) =>
{   
    LineCountData data = new LineCountData(path, filter, lineFilter, exceptFilter, exceptLineFilter);    
    var result = await LineCount.LineCount.GetLineCount(data, exludeDirectories, exludeFilesOption);

    result.Match(
        lineCount => Console.WriteLine($"{lineCount} lines found."),
        error => Console.Error.WriteLine($"Directory '{error.Path}' was not found.")
        );
}, pathArgument, filterOption, lineFilterOption, exceptFilterOption, exceptLineFilterOption, exludeDirectoriesOption, exludeFilesOption);
return await rootCommand.InvokeAsync(args);