using Cocona;

namespace LoadTest.Models;

public class SaveUrlsOptions : ICommandParameterSet
{
    [Option("path", ['p'], Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; set; } = string.Empty;

    [Option("output", ['o'], Description = "File path to save output to.", ValueName = "output")]
    public string OutputPath { get; init; } = string.Empty;

    [Option("header", Description = "Custom headers to include in requests. Format: \"Key: Value\".", ValueName = "header")]
    [HasDefaultValue]
    public List<string>? CustomHeaders { get; init; }

    [Option("user-agent", Description = "User-Agent to use for requests.", ValueName = "user-agent")]
    [HasDefaultValue]
    public string? UserAgent { get; init; }
}
