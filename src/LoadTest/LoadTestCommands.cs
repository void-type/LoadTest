using Cocona;
using Cocona.Application;
using LoadTest.Models;
using LoadTest.Services;

namespace LoadTest;

public class LoadTestCommands
{
    private readonly ICoconaAppContextAccessor _contextAccessor;

    public LoadTestCommands(ICoconaAppContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public CancellationToken CancellationToken => _contextAccessor?.Current?.CancellationToken ?? CancellationToken.None;

    [Command("save-urls", Description = "Save the sitemap as a list of URLs. Speeds up repeat runs.")]
    public async Task MakeListAsync(SaveUrlsOptions options, [FromService] UrlsRetriever urlsRetriever)
    {
        await urlsRetriever.SaveUrlsAsync(options.SitemapUrl, options.OutputPath, CancellationToken);
    }

    [Command("load", Description = "Run a load test on a given set of URLs. Does not spider.")]
    public async Task RunAsync(LoadTestOptions options, [FromService] LoadTester loadTester)
    {
        await loadTester.RunLoadTestAsync(options, CancellationToken);
    }

    [Command("archive", Description = "Save the HTML of pages.")]
    public async Task ArchivePagesAsync(PageArchiveOptions options, [FromService] PageArchiver pageArchiver)
    {
        await pageArchiver.ArchiveHtmlAsync(options, CancellationToken);
    }
}
