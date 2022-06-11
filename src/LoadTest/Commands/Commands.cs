using System.CommandLine;
using System.CommandLine.Invocation;
using LoadTest.Services;

namespace LoadTest.Commands;

public static class Commands
{
    static Commands()
    {
        RunCommand = new Command("run", "Run a load test.")
        {
            CommandOptions.PathOption,
            CommandOptions.ThreadCountOption,
            CommandOptions.SecondsToRunOption,
            CommandOptions.ChanceOf404Option,
            CommandOptions.DelayOption,
            CommandOptions.VerboseOption,
        };

        RunCommand.AddAlias("r");

        RunCommand.SetHandler(async (InvocationContext context) =>
        {
            var config = new LoadTesterConfiguration
            {
                Path = context.ParseResult.GetValueForOption(CommandOptions.PathOption) ?? throw new InvalidOperationException(),
                ThreadCount = context.ParseResult.GetValueForOption(CommandOptions.ThreadCountOption),
                SecondsToRun = context.ParseResult.GetValueForOption(CommandOptions.SecondsToRunOption),
                ChanceOf404 = context.ParseResult.GetValueForOption(CommandOptions.ChanceOf404Option),
                IsDelayEnabled = context.ParseResult.GetValueForOption(CommandOptions.DelayOption),
                IsVerbose = context.ParseResult.GetValueForOption(CommandOptions.VerboseOption),
            };

            context.ExitCode = await LoadTester.RunLoadTest(config);
        });

        MakeListCommand = new Command("make-list", "Save the sitemap as a list of URLs. Speeds up repeat runs.")
        {
            CommandOptions.PathOption,
            CommandOptions.OutputPathOption,
        };

        MakeListCommand.AddAlias("ml");

        MakeListCommand.SetHandler(async (InvocationContext context) =>
        {
            string path = context.ParseResult.GetValueForOption(CommandOptions.PathOption) ?? throw new InvalidOperationException();
            string outputPath = context.ParseResult.GetValueForOption(CommandOptions.OutputPathOption) ?? throw new InvalidOperationException();

            context.ExitCode = await UrlsRetriever.SaveUrls(path, outputPath);
        });
    }

    public static Command RunCommand { get; }
    public static Command MakeListCommand { get; }
}
