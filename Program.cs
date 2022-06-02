﻿using System.CommandLine;
using LoadTest;

// TODO: spectre output?
// TODO: git versioning and build

var modeOption = new Option<string>(
    name: "--mode",
    description: "Choose a run mode")
{
    IsRequired = true,
}
    .FromAmong("sitemap-url", "sitemap-file", "url-list-file");

var targetListOption = new Option<string>(
    name: "--target-list",
    description: "Location of the target list, dependent on the mode.")
{
    IsRequired = true
};

var threadCountOption = new Option<int>(
    name: "--threads",
    description: "Number of threads to run",
    getDefaultValue: () => 2);

threadCountOption.AddValidator(result =>
{
    if (result.GetValueForOption(threadCountOption) < 1)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than 0";
    }
});

var secondsToRunOption = new Option<int>(
    name: "--seconds",
    description: "Number of seconds to run before stopping",
    getDefaultValue: () => 5);

secondsToRunOption.AddValidator(result =>
{
    if (result.GetValueForOption(secondsToRunOption) < 1)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than 0";
    }
});

var chanceOf404Option = new Option<int>(
    name: "--chance-404",
    description: "Percent chance of an intentional page miss.",
    getDefaultValue: () => 0);

chanceOf404Option.AddValidator(result =>
{
    if (result.GetValueForOption(chanceOf404Option) < 0)
    {
        var optionName = result.Option.Name;
        result.ErrorMessage = $"{optionName} must be greater than or equal to 0";
    }
});

var slowOption = new Option<bool>(
    name: "--slow",
    description: "Add delay between requests");

var rootCommand = new RootCommand(description: "Simple website load tester")
{
    modeOption,
    targetListOption,
    threadCountOption,
    secondsToRunOption,
    chanceOf404Option,
    slowOption,
};

rootCommand.Name = "loadtest";

rootCommand.SetHandler(
    async (string mode, string targetList, int threadCount, int secondsToRun, int chanceOf404, bool isSlowEnabled) =>
    {
        await LoadTestHelpers.RunLoadTest(mode, targetList, threadCount, secondsToRun, chanceOf404, isSlowEnabled);
    },
    modeOption,
    targetListOption,
    threadCountOption,
    secondsToRunOption,
    chanceOf404Option,
    slowOption
);

return await rootCommand.InvokeAsync(args);
