using System.CommandLine;
using LoadTest.Commands;

var rootCommand = new RootCommand(description: "Simple website load tester.");
rootCommand.Name = "load-test";

rootCommand.Add(Commands.LoadTestCommand);
rootCommand.Add(Commands.MakeListCommand);
rootCommand.Add(Commands.ArchivePagesCommand);

return await rootCommand.InvokeAsync(args);
