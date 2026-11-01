using Cocona;
using Cocona.Application;
using LoadTest.Helpers;
using LoadTest.Models;
using LoadTest.Services;

namespace LoadTest;

[CommandTitle]
public class LoadTestCommands(ICoconaAppContextAccessor contextAccessor)
{
    public CancellationToken CancellationToken => contextAccessor?.Current?.CancellationToken ?? CancellationToken.None;

    [Command("save-urls", Description = "Save the sitemap as a list of URLs. Speeds up repeat runs.")]
    public async Task MakeListAsync(SaveUrlsOptions options, [FromService] UrlsRetriever urlsRetriever)
    {
        await urlsRetriever.SaveUrlsAsync(options.SitemapUrl, options.OutputPath, options.CustomHeaders, options.UserAgent,
            options.PrimaryDomain, options.PrimaryDomainEquivalents, CancellationToken);
    }

    [Command("load", Description = "Run a load test on a given set of URLs. Does not spider. While running, press p to pause, r to resume, or q to stop early and show results.")]
    public async Task RunAsync(LoadTestOptions options, [FromService] LoadTester loadTester)
    {
        await loadTester.RunLoadTestAsync(options, CancellationToken);
    }

    [Command("archive", Description = "Save the HTML of pages. While running, press p to pause, r to resume, or q to stop early and save results.")]
    public async Task ArchivePagesAsync(PageArchiveOptions options, [FromService] PageArchiver pageArchiver)
    {
        await pageArchiver.ArchiveHtmlAsync(options, CancellationToken);
    }
}
