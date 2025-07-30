using Cocona;

namespace LoadTest.Models;

public class LoadTestOptions : ICommandParameterSet
{
    [Option("path", ['p'], Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; set; } = string.Empty;

    [Option("threads", ['t'], Description = "Number of concurrent threads to make requests.", ValueName = "threads")]
    [HasDefaultValue]
    public int ThreadCount { get; init; } = 2;

    [Option("seconds", ['s'], Description = "Number of seconds to run before stopping. If zero, requests all URLs once.", ValueName = "seconds")]
    [HasDefaultValue]
    public int SecondsToRun { get; init; } = 5;

    [Option("chance-404", ['e'], Description = "Percent chance of an intentional page miss.", ValueName = "chance-404")]
    [HasDefaultValue]
    public int ChanceOf404 { get; init; }

    [Option("delay", ['d'], Description = "Add delay between requests.", ValueName = "delay")]
    public bool IsDelayEnabled { get; init; }

    [Option("method", ['m'], Description = "Change the request method.", ValueName = "method")]
    [HasDefaultValue]
    public HttpMethod RequestMethod { get; init; } = HttpMethod.Get;

    [Option("verbose", ['v'], Description = "Show more logging.", ValueName = "verbose")]
    public bool IsVerbose { get; init; }
}
