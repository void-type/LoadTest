using System.CommandLine;
using LoadTest.Commands;

var rootCommand = new RootCommand(description: "Simple website load tester.");
rootCommand.Name = "load-test";

rootCommand.Add(Commands.RunCommand);
rootCommand.Add(Commands.MakeListCommand);

return await rootCommand.InvokeAsync(args);
