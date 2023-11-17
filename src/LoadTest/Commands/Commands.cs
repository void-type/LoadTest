using LoadTest.Services;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace LoadTest.Commands;

public static class Commands
{
    static Commands()
    {
        LoadTestCommand = BuildLoadTestCommand();
        MakeListCommand = BuildMakeListCommand();
        ArchivePagesCommand = BuildArchivePagesCommand();
    }

    private static Command BuildLoadTestCommand()
    {
        var runCommand = new Command("run", "Run a load test.")
        {
            CommandOptions.PathOption,
            CommandOptions.ThreadCountOption,
            CommandOptions.SecondsToRunOption,
            CommandOptions.ChanceOf404Option,
            CommandOptions.DelayOption,
            CommandOptions.RequestMethodOption,
            CommandOptions.VerboseOption,
        };

        runCommand.AddAlias("r");

        runCommand.SetHandler(async (InvocationContext context) =>
        {
            var config = new LoadTesterConfiguration
            {
                ThreadCount = context.GetValueForOptionEnsureNotNull(CommandOptions.ThreadCountOption),
                SecondsToRun = context.GetValueForOptionEnsureNotNull(CommandOptions.SecondsToRunOption),
                ChanceOf404 = context.GetValueForOptionEnsureNotNull(CommandOptions.ChanceOf404Option),
                IsDelayEnabled = context.GetValueForOptionEnsureNotNull(CommandOptions.DelayOption),
                RequestMethod = context.GetValueForOptionEnsureNotNull(CommandOptions.RequestMethodOption) switch
                {
                    "HEAD" => HttpMethod.Head,
                    _ => HttpMethod.Get,
                },
                IsVerbose = context.GetValueForOptionEnsureNotNull(CommandOptions.VerboseOption),
            };

            var path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            var urls = await UrlsRetriever.GetUrlsAsync(path, context.GetCancellationToken());

            context.ExitCode = await LoadTester.RunLoadTestAsync(config, urls, context.GetCancellationToken());
        });

        return runCommand;
    }

    private static Command BuildMakeListCommand()
    {
        var makeListCommand = new Command("make-list", "Save the sitemap as a list of URLs. Speeds up repeat runs.")
        {
            CommandOptions.PathOption,
            CommandOptions.OutputPathOption,
        };

        makeListCommand.AddAlias("ml");

        makeListCommand.SetHandler(async (InvocationContext context) =>
        {
            var path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            var outputPath = context.GetValueForOptionEnsureNotNull(CommandOptions.OutputPathOption);

            context.ExitCode = await UrlsRetriever.SaveUrlsAsync(path, outputPath, context.GetCancellationToken());
        });

        return makeListCommand;
    }

    private static Command BuildArchivePagesCommand()
    {
        var archivePagesCommand = new Command("archive-pages", "Save the html of pages.")
        {
            CommandOptions.PathOption,
            CommandOptions.OutputPathOption,
            CommandOptions.ThreadCountOption,
            CommandOptions.DelayOption,
            CommandOptions.UseBrowserOption,
            CommandOptions.LogBrowserConsoleError,
            CommandOptions.VerboseOption,
        };

        archivePagesCommand.AddAlias("ar");

        archivePagesCommand.SetHandler(async (InvocationContext context) =>
        {
            var config = new PageArchiverConfiguration
            {
                OutputPath = context.GetValueForOptionEnsureNotNull(CommandOptions.OutputPathOption),
                ThreadCount = context.GetValueForOptionEnsureNotNull(CommandOptions.ThreadCountOption),
                IsDelayEnabled = context.GetValueForOptionEnsureNotNull(CommandOptions.DelayOption),
                UseBrowser = context.GetValueForOptionEnsureNotNull(CommandOptions.UseBrowserOption),
                LogBrowserConsoleError = context.GetValueForOptionEnsureNotNull(CommandOptions.LogBrowserConsoleError),
                IsVerbose = context.GetValueForOptionEnsureNotNull(CommandOptions.VerboseOption),
            };

            var path = context.GetValueForOptionEnsureNotNull(CommandOptions.PathOption);
            var urls = await UrlsRetriever.GetUrlsAsync(path, context.GetCancellationToken());

            context.ExitCode = await PageArchiver.ArchiveHtmlAsync(config, urls, context.GetCancellationToken());
        });

        return archivePagesCommand;
    }

    public static Command LoadTestCommand { get; }
    public static Command MakeListCommand { get; }
    public static Command ArchivePagesCommand { get; }
}
