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
    }

    private static Option<string> BuildPathOption()
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "URL list file path or sitemap URL.")
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
            description: "Number of concurrent threads to make requests.",
            getDefaultValue: () => 2)
        {
        };

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
            description: "Number of seconds to run before stopping. If zero, requests all URLs once.",
            getDefaultValue: () => 5)
        {
        };

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
            description: "Percent chance of an intentional page miss.",
            getDefaultValue: () => 0)
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

    public static Option<string> PathOption { get; }
    public static Option<string> OutputPathOption { get; }
    public static Option<int> ThreadCountOption { get; }
    public static Option<int> SecondsToRunOption { get; }
    public static Option<int> ChanceOf404Option { get; }
    public static Option<bool> DelayOption { get; }
    public static Option<bool> VerboseOption { get; }
}
