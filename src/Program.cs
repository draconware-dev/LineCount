using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using LineCount.CLI;

RootCommand rootCommand = new LinecountCommand();

CommandLineBuilder builder = new CommandLineBuilder(rootCommand)
    .UseVersionOption("--version", "-v")
    .UseDefaults();

var parser = builder.Build();

return await parser.InvokeAsync(args);