using System.CommandLine;

var rootCommand = new RootCommand("Line Count");
var pathArgument = new Argument<string>("path", "The path of the directory that contains the files to calculate the count of. Use '.' to refer to the current directory. ");
rootCommand.AddArgument(pathArgument); 
    rootCommand.SetHandler(async (path) =>
    {    
        int lineCount = await LineCount.GetLineCount(path);
        Console.WriteLine($"{lineCount} lines found.   ");
    }, pathArgument);