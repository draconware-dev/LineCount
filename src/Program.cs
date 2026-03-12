using System.CommandLine;
using Linecount.CLI;

RootCommand rootCommand = new LocRootCommand();

var parser = rootCommand.Parse(args);

return await parser.InvokeAsync();