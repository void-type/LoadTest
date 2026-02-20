using Cocona;

namespace LoadTest.Models;

public class PageArchiveOptions : ICommandParameterSet
{
    [Option("path", ['p'], Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; init; } = string.Empty;

    [Option("exclude-urls-regex", Description = "Exclude URL paths using regular expressions.", ValueName = "regex")]
    [HasDefaultValue]
    public List<string>? ExcludedUrlsRegexPatterns { get; init; }

    [Option("output", ['o'], Description = "File path to save output to.", ValueName = "folder path")]
    public string OutputPath { get; init; } = string.Empty;

    [Option("browser", ['b'], Description = "Use browser (Puppeteer) to fully render pages, including JS. Otherwise only HTML source is considered.")]
    public bool UseBrowser { get; init; }

    [Option("browser-errors", ['e'], Description = "Log browser console errors.")]
    public bool LogBrowserConsoleErrors { get; init; }

    [Option("threads", ['t'], Description = "Number of concurrent threads to make requests.", ValueName = "count")]
    [HasDefaultValue]
    public int ThreadCount { get; init; } = 2;

    [Option("delay", ['d'], Description = "Add delay between requests.")]
    public bool IsDelayEnabled { get; init; }

    [Option("verbose", ['v'], Description = "Show more logging.")]
    public bool IsVerbose { get; init; }

    [Option("spider", Description = "Enable spider (find local pages linked from other pages). Limited to the primary domain or equivalent.")]
    public bool IsSpiderEnabled { get; init; }

    [Option("domain", Description = "Primary domain to archive.", ValueName = "example.com")]
    [HasDefaultValue]
    public string? PrimaryDomain { get; init; }

    [Option("domain-alts", Description = "Primary domain equivalents.", ValueName = "www.example.com")]
    [HasDefaultValue]
    public string[]? PrimaryDomainEquivalents { get; init; }

    [Option("cross-domain", Description = "Scan cross domain redirects. Note that spider link finding is still limited to only the primary domain.")]
    public bool ScanCrossDomainRedirects { get; init; }

    [Option("ignore-content-type", Description = "Ignore the Content-Type header when archiving. Otherwise only text/html is saved.")]
    public bool IgnoreContentType { get; init; }

    [Option("content-search-include", Description = "Only search content within this CSS selector. For example, body or main. Whole document is saved by default.", ValueName = "main-content")]
    [HasDefaultValue]
    public string? ContentIncludeSelector { get; init; }

    [Option("content-search-exclude", Description = "Exclude elements from the include content that match this CSS selector.", ValueName = "exclude-content")]
    [HasDefaultValue]
    public string? ContentExcludeSelector { get; init; }

    [Option("content-search-terms", Description = "Search content for specific terms. Results are output to the CSV.", ValueName = "search term")]
    [HasDefaultValue]
    public List<string>? ContentSearchTerms { get; init; }

    [Option("only-save-if-term-found", Description = "Only save content if one of the search terms is found.")]
    [HasDefaultValue]
    public bool OnlySaveIfTermFound { get; init; }

    [Option("user-agent", Description = "User-Agent to use for requests.", ValueName = "user-agent")]
    [HasDefaultValue]
    public string? UserAgent { get; init; }

    [Option("header", Description = "Custom headers to include in requests. Format: \"Key: Value\".", ValueName = "header")]
    [HasDefaultValue]
    public List<string>? CustomHeaders { get; init; }
}
