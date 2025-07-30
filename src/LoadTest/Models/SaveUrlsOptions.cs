using Cocona;

namespace LoadTest.Models;

public class SaveUrlsOptions : ICommandParameterSet
{
    [Option("path", ['p'], Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; set; } = string.Empty;

    [Option("output", ['o'], Description = "File path to save output to.", ValueName = "output")]
    public string OutputPath { get; init; } = string.Empty;
}
