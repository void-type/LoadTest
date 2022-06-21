using System.CommandLine;

namespace LoadTest.Commands;

public static class CommandOptions
{
    static CommandOptions()
    {
        PathOption = new Option<string>(
            name: "--path",
            description: "URL list file path or sitemap URL.")
        {
            IsRequired = true,
            ArgumentHelpName = "file path or URL",
        };

        PathOption.AddAlias("-p");

        OutputPathOption = new Option<string>(
            name: "--output",
            description: "File path to save output to.")
        {
            IsRequired = true,
            ArgumentHelpName = "file path",
        };

        OutputPathOption.AddAlias("-o");

        ThreadCountOption = new Option<int>(
            name: "--threads",
            description: "Number of concurrent threads to make requests.",
            getDefaultValue: () => 2)
        {
        };

        ThreadCountOption.AddAlias("-t");

        ThreadCountOption.AddValidator(result =>
        {
            if (result.GetValueForOption(ThreadCountOption) < 1)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be greater than 0.";
            }
        });

        SecondsToRunOption = new Option<int>(
            name: "--seconds",
            description: "Number of seconds to run before stopping. If zero, requests all URLs once.",
            getDefaultValue: () => 5)
        {
        };

        SecondsToRunOption.AddAlias("-s");

        SecondsToRunOption.AddValidator(result =>
        {
            if (result.GetValueForOption(SecondsToRunOption) < 0)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be greater than or equal to 0.";
            }
        });

        ChanceOf404Option = new Option<int>(
            name: "--chance-404",
            description: "Percent chance of an intentional page miss.",
            getDefaultValue: () => 0)
        {
            ArgumentHelpName = "percent",
        };

        ChanceOf404Option.AddAlias("-e");

        ChanceOf404Option.AddValidator(result =>
        {
            if (result.GetValueForOption(ChanceOf404Option) < 0)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be greater than or equal to 0.";
            }

            if (result.GetValueForOption(ChanceOf404Option) > 100)
            {
                var optionName = result.Option.Name;
                result.ErrorMessage = $"{optionName} must be less than or equal to 100.";
            }
        });

        DelayOption = new Option<bool>(
            name: "--delay",
            description: "Add delay between requests.");

        DelayOption.AddAlias("-d");

        VerboseOption = new Option<bool>(
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
