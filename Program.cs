using System.CommandLine;
using LoadTest;

// TODO: spectre output?

var modeOption = new Option<LoadTesterMode>(
    name: "--mode",
    description: "Target list type")
{
    IsRequired = true,
};

var targetListOption = new Option<string>(
    name: "--target-list",
    description: "Target list path (dependent on the mode)")
{
    IsRequired = true
};

var threadCountOption = new Option<int>(
    name: "--threads",
    description: "Number of concurrent threads to make requests",
    getDefaultValue: () => 2)
{
};

threadCountOption.AddValidator(result =>
{
    if (result.GetValueForOption(threadCountOption) < 1)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than 0.";
    }
});

var secondsToRunOption = new Option<int>(
    name: "--seconds",
    description: "Number of seconds to run before stopping",
    getDefaultValue: () => 5)
{
};

secondsToRunOption.AddValidator(result =>
{
    if (result.GetValueForOption(secondsToRunOption) < 1)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than 0.";
    }
});

var chanceOf404Option = new Option<int>(
    name: "--chance-404",
    description: "Percent chance of an intentional page miss",
    getDefaultValue: () => 0);

chanceOf404Option.AddValidator(result =>
{
    if (result.GetValueForOption(chanceOf404Option) < 0)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than or equal to 0.";
    }

    if (result.GetValueForOption(chanceOf404Option) > 100)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be less than or equal to 100.";
    }
});

var slowOption = new Option<bool>(
    name: "--slow",
    description: "Add delay between requests");

var allOnceOption = new Option<bool>(
    name: "--all-once",
    description: "Request each URL once. Ignores --seconds. Good for 404 checking.");

var makeUrlListOption = new Option<string>(
    name: "--make-list",
    description: "Instead of running tests, reads the target list and outputs it to a local file. Useful to speed up repeated runs with slow sitemap retrieval.");

var verboseOption = new Option<bool>(
    name: "--verbose",
    description: "Show more logging");

var rootCommand = new RootCommand(
    description:
@"Simple website load tester.

Modes to get URL to test against:
  * Sitemap - a Sitemap.xml hosted on a web server. Any sitemap indexes will be crawled recursively. Retrieved over https.
  * UrlList - a file with one URL per line. Retrieved via file protocols.")
{
    modeOption,
    targetListOption,
    threadCountOption,
    secondsToRunOption,
    chanceOf404Option,
    slowOption,
    verboseOption,
    allOnceOption,
    makeUrlListOption,
};

rootCommand.Name = "loadtest";

rootCommand.SetHandler(
    async (
        LoadTesterMode mode,
         string targetList,
        int threadCount,
        int secondsToRun,
        int chanceOf404,
        bool isSlowEnabled,
        bool isVerbose,
        bool isAllOnce,
        string makeUrlList) =>
    {
        var options = new LoadTesterOptions
        {
            Mode = mode,
            TargetList = targetList,
            ThreadCount = threadCount,
            SecondsToRun = secondsToRun,
            ChanceOf404 = chanceOf404,
            IsSlowEnabled = isSlowEnabled,
            IsVerbose = isVerbose,
            IsAllOnce = isAllOnce,
            MakeUrlList = makeUrlList,
        };

        await LoadTester.RunLoadTest(options);
    },
    modeOption,
    targetListOption,
    threadCountOption,
    secondsToRunOption,
    chanceOf404Option,
    slowOption,
    verboseOption,
    allOnceOption,
    makeUrlListOption
);

return await rootCommand.InvokeAsync(args);
