using System.CommandLine;

namespace LoadTest.Commands;

public static class CommandOptions
{
    static CommandOptions()
    {
        PathOption = BuildPathOption();
        OutputPathOption = BuildOutputPathOption();
        ThreadCountOption = BuildThreadCountOption();
        SecondsToRunOption = BuildSecondsToRunOption();
        ChanceOf404Option = BuildChanceOf404Option();
        DelayOption = BuildDelayOption();
        VerboseOption = BuildVerboseOption();
        RequestMethodOption = BuildRequestMethodOption();
        UseBrowserOption = BuildBrowserOption();
    }

    private static Option<string> BuildPathOption()
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.")
        {
            IsRequired = true,
            ArgumentHelpName = "file path or URL",
        };

        pathOption.AddAlias("-p");

        return pathOption;
    }

    private static Option<string> BuildOutputPathOption()
    {
        var outputPathOption = new Option<string>(
            name: "--output",
            description: "File path to save output to.")
        {
            IsRequired = true,
            ArgumentHelpName = "file path",
        };

        outputPathOption.AddAlias("-o");

        return outputPathOption;
    }

    private static Option<int> BuildThreadCountOption()
    {
        var threadCountOption = new Option<int>(
            name: "--threads",
            getDefaultValue: () => 2,
            description: "Number of concurrent threads to make requests.");

        threadCountOption.AddAlias("-t");

        threadCountOption.AddValidator(result =>
        {
            if (result.GetValueForOption(threadCountOption) < 1)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be greater than 0.";
            }
        });

        return threadCountOption;
    }

    private static Option<int> BuildSecondsToRunOption()
    {
        var secondsToRunOption = new Option<int>(
            name: "--seconds",
            getDefaultValue: () => 5,
            description: "Number of seconds to run before stopping. If zero, requests all URLs once.");

        secondsToRunOption.AddAlias("-s");

        secondsToRunOption.AddValidator(result =>
        {
            if (result.GetValueForOption(secondsToRunOption) < 0)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be greater than or equal to 0.";
            }
        });

        return secondsToRunOption;
    }

    private static Option<int> BuildChanceOf404Option()
    {
        var chanceOf404Option = new Option<int>(
            name: "--chance-404",
            getDefaultValue: () => 0,
            description: "Percent chance of an intentional page miss.")
        {
            ArgumentHelpName = "percent",
        };

        chanceOf404Option.AddAlias("-e");

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

        return chanceOf404Option;
    }

    private static Option<bool> BuildDelayOption()
    {
        var delayOption = new Option<bool>(
            name: "--delay",
            description: "Add delay between requests.");

        delayOption.AddAlias("-d");

        return delayOption;
    }

    private static Option<bool> BuildVerboseOption()
    {
        return new Option<bool>(
            name: "--verbose",
            description: "Show more logging.");
    }

    private static Option<string> BuildRequestMethodOption()
    {
        var requestMethodOption = new Option<string>(
            name: "--method",
            getDefaultValue: () => "GET",
            description: "Change the request method.");

        requestMethodOption.FromAmong("HEAD", "GET");
        requestMethodOption.AddAlias("-m");

        return requestMethodOption;
    }

    private static Option<bool> BuildBrowserOption()
    {
        var delayOption = new Option<bool>(
            name: "--browser",
            description: "Use browser (Puppeteer) to fully render pages, including JS.");

        delayOption.AddAlias("-b");

        return delayOption;
    }

    public static Option<string> PathOption { get; }
    public static Option<string> OutputPathOption { get; }
    public static Option<int> ThreadCountOption { get; }
    public static Option<int> SecondsToRunOption { get; }
    public static Option<int> ChanceOf404Option { get; }
    public static Option<bool> DelayOption { get; }
    public static Option<bool> VerboseOption { get; }
    public static Option<string> RequestMethodOption { get; }
    public static Option<bool> UseBrowserOption { get; }
}
