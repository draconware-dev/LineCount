using System.CommandLine;
using System.CommandLine.Parsing;
using LineCount.CLI;

RootCommand rootCommand = new LinecountCommand();

var parser = rootCommand.Parse(args);

return await parser.InvokeAsync();