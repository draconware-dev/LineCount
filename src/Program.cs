using System.CommandLine;
using LineCount;

var rootCommand = new RootCommand("Line Count");

var filterOption = new Option<string>(["-f", "--filter"], "a glob-pattern for files to include.");
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

rootCommand.SetHandler(async (path, filter, exludeDirectories, exludeFilesOption) =>
{
    int lineCount = await LineCount.LineCount.GetLineCount(path, filter, exludeDirectories, exludeFilesOption);
    Console.WriteLine($"{lineCount} lines found.");
}, pathArgument, filterOption, exludeDirectoriesOption, exludeFilesOption);
return await rootCommand.InvokeAsync(args);