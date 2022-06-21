using System.CommandLine;
using System.CommandLine.Invocation;
using LoadTest.Services;

namespace LoadTest.Commands;

public static class Commands
{
    static Commands()
    {
        // Run load test
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
                ThreadCount = context.GetValueForOptionEnsureNotNull(CommandOptions.ThreadCountOption),
                SecondsToRun = context.GetValueForOptionEnsureNotNull(CommandOptions.SecondsToRunOption),
                ChanceOf404 = context.GetValueForOptionEnsureNotNull(CommandOptions.ChanceOf404Option),
                IsDelayEnabled = context.GetValueForOptionEnsureNotNull(CommandOptions.DelayOption),
                IsVerbose = context.GetValueForOptionEnsureNotNull(CommandOptions.VerboseOption),
            };

            var path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            var urls = await UrlsRetriever.GetUrls(path);

            context.ExitCode = LoadTester.RunLoadTest(config, urls);
        });

        // Make list
        MakeListCommand = new Command("make-list", "Save the sitemap as a list of URLs. Speeds up repeat runs.")
        {
            CommandOptions.PathOption,
            CommandOptions.OutputPathOption,
        };

        MakeListCommand.AddAlias("ml");

        MakeListCommand.SetHandler(async (InvocationContext context) =>
        {
            string path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            string outputPath = context.GetValueForOptionEnsureNotNull(CommandOptions.OutputPathOption);

            context.ExitCode = await UrlsRetriever.SaveUrls(path, outputPath);
        });

        // Archive pages
        ArchivePagesCommand = new Command("archive-pages", "Save the html from the request.")
        {
            CommandOptions.PathOption,
            CommandOptions.OutputPathOption,
            CommandOptions.ThreadCountOption,
            CommandOptions.DelayOption,
            CommandOptions.VerboseOption,
        };

        ArchivePagesCommand.AddAlias("ar");

        ArchivePagesCommand.SetHandler(async (InvocationContext context) =>
        {
            var config = new PageArchiverConfiguration
            {
                OutputPath = context.GetValueForOptionEnsureNotNull(CommandOptions.OutputPathOption),
                ThreadCount = context.GetValueForOptionEnsureNotNull(CommandOptions.ThreadCountOption),
                IsDelayEnabled = context.GetValueForOptionEnsureNotNull(CommandOptions.DelayOption),
                IsVerbose = context.GetValueForOptionEnsureNotNull(CommandOptions.VerboseOption),
            };

            var path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            var urls = await UrlsRetriever.GetUrls(path);

            context.ExitCode = PageArchiver.ShallowArchive(config, urls);
        });
    }

    public static Command RunCommand { get; }
    public static Command MakeListCommand { get; }
    public static Command ArchivePagesCommand { get; }
}
