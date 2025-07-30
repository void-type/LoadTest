using LoadTest.Helpers;
using LoadTest.Models;
using System.Diagnostics;
using VoidCore.Model.Text;

namespace LoadTest.Services;

public class PageArchiver
{
    private readonly UrlsRetriever _urlsRetriever;
    private readonly HtmlContentRetriever _htmlContentRetriever;

    public PageArchiver(UrlsRetriever urlsRetriever, HtmlContentRetriever htmlContentRetriever)
    {
        _urlsRetriever = urlsRetriever;
        _htmlContentRetriever = htmlContentRetriever;
    }

    /// <summary>
    /// Saves a copy of the page HTML.
    /// </summary>
    public async Task ArchiveHtmlAsync(PageArchiveOptions options, CancellationToken cancellationToken)
    {
        var csvFilePath = $"{options.OutputPath.TrimEnd('/')}/{DateTime.Now:yyyyMMdd_HHmmss}_{nameof(PageArchiver)}.csv";

        var uris = (await _urlsRetriever.GetUrlsAsync(options.SitemapUrl, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.GetNormalizedUri(options.PrimaryDomain, options.PrimaryDomainEquivalents, null))
            .Where(x => x is not null && PathIsNotExcluded(options, x))
            .Distinct()
            .Select(x => x!)
            .ToArray();

        if (uris.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return;
        }

        Console.WriteLine("Performing HTML archive. Press Ctrl+C to stop.");

        var startTime = Stopwatch.GetTimestamp();

        var jobResult = new PageArchiveResult();
        var spiderLinksCount = 0;
        var passes = 0;

        try
        {
            do
            {
                passes++;

                var isSpiderPass = passes > 1;

                var tasks = Enumerable
                    .Range(0, options.ThreadCount)
                    .Select(i => StartThreadAsync(i, uris, options, _htmlContentRetriever, isSpiderPass, cancellationToken))
                    .ToArray();

                var passResult = (await Task.WhenAll(tasks)).Combine();

                var previousUrls = jobResult.PageResults
                    .Select(y => y.Url.ToString())
                    .ToArray();

                var newSpiderLinks = passResult.PageResults
                    .SelectMany(x => x.SpiderLinks)
                    .Distinct()
                    .Where(x => x is not null && PathIsNotExcluded(options, x) && !Array.Exists(previousUrls, y => y.EqualsIgnoreCase(x.ToString())))
                    .ToArray();

                uris = newSpiderLinks;
                spiderLinksCount += newSpiderLinks.Length;

                jobResult = jobResult.Combine(passResult);

            } while (uris.Length > 0 && !cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Cancelled.");
        }
        else
        {
            Console.WriteLine("Finished.");
        }

        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        var seconds = elapsedTime.TotalMilliseconds / 1000;
        var safeSeconds = seconds < 1 ? 1 : seconds;

        Console.WriteLine($"{jobResult.RequestCount} requests in {elapsedTime} = {jobResult.RequestCount / safeSeconds:F2} RPS");

        if (options.IsSpiderEnabled)
        {
            Console.WriteLine($"Spider found {spiderLinksCount} pages that weren't in the original list. Took {passes} passes.");
        }

        var missedPercent = (double)jobResult.RetrieveErrorCount / jobResult.RequestCount * 100;
        Console.WriteLine($"{jobResult.RetrieveErrorCount} retrieve errors = {missedPercent:F2}%");

        var scanErrorPercent = (double)jobResult.ScanErrorCount / jobResult.RequestCount * 100;
        Console.WriteLine($"{jobResult.ScanErrorCount} scan errors = {scanErrorPercent:F2}%");

        var otherErrorPercent = (double)jobResult.OtherErrorCount / jobResult.RequestCount * 100;
        Console.WriteLine($"{jobResult.OtherErrorCount} other errors = {otherErrorPercent:F2}%");

        await FileHelper.SaveResultsCsvAsync(csvFilePath, jobResult);
    }

    private static async Task<PageArchiveResult> StartThreadAsync(int threadNumber, Uri[] urls, PageArchiveOptions options, HtmlContentRetriever client, bool isSpiderPass, CancellationToken cancellationToken)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, options.ThreadCount, urls.Length);

        var threadResult = new PageArchiveResult();

        if (initialUrlIndex == -1)
        {
            return threadResult;
        }

        var urlIndex = initialUrlIndex;

        while (!cancellationToken.IsCancellationRequested)
        {
            var uri = urls[urlIndex] ?? throw new InvalidOperationException($"URL at index {urlIndex} is null.");

            var pageResult = await ProcessPageAsync(options, client, uri, isSpiderPass, cancellationToken);

            threadResult.PageResults.Add(pageResult);

            if (urlIndex == stopUrlIndex)
            {
                break;
            }

            // Get the next URL, looping around to beginning if at the end.
            urlIndex++;

            if (options.IsDelayEnabled)
            {
                try
                {
                    await Task.Delay(500, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // If the delay is cancelled, we just exit the loop.
                    break;
                }
            }
        }

        if (options.IsVerbose)
        {
            Console.WriteLine($"Thread {threadNumber} ending.");
        }

        return threadResult;
    }

    private static async Task<PageArchivePageResult> ProcessPageAsync(PageArchiveOptions options, HtmlContentRetriever client, Uri uri, bool isSpiderPass, CancellationToken cancellationToken)
    {
        var pageResult = new PageArchivePageResult(uri)
        {
            IsOnlyFoundBySpider = isSpiderPass
        };

        var pageUrl = uri.ToString();

        try
        {
            var page = await client.GetContentAsync(options, uri, cancellationToken);

            if (page.FinalUrl is not null)
            {
                // Uri equivalency uses .ToString()
                // We can assume they are both normalized, so we don't need to ignore casing.
                pageResult.FinalUrl = page.FinalUrl;
                pageResult.IsRedirected = uri != pageResult.FinalUrl;
                pageResult.IsCrossDomainRedirect = pageResult.FinalUrl.Host != uri.Host;
            }

            pageResult.IsRetrieveError = page.IsRetrieveError;
            pageResult.StatusCode = page.StatusCode;

            if (pageResult.IsRetrieveError)
            {
                return pageResult;
            }

            if (!options.ScanCrossDomainRedirects && pageResult.IsCrossDomainRedirect)
            {
                if (options.IsVerbose)
                {
                    Console.WriteLine($"Skipping save for {uri} as it is a cross-domain redirect.");
                }

                return pageResult;
            }

            var scanResult = await HtmlScanner.ScanAsync(options, pageUrl, page.HtmlContent, cancellationToken);

            pageResult.IsScanError = scanResult.IsScanError;
            pageResult.SpiderLinks = scanResult.SpiderLinks;
            pageResult.WasSearchTermsFound = scanResult.SearchTermsFoundInHtml.Count > 0 || scanResult.SearchTermsFoundInText.Count > 0;
            pageResult.SearchTermsFoundInHtml = scanResult.SearchTermsFoundInHtml;
            pageResult.SearchTermsFoundInText = scanResult.SearchTermsFoundInText;

            if (options.OnlySaveIfTermFound)
            {
                if (!pageResult.WasSearchTermsFound)
                {
                    return pageResult;
                }

                Console.WriteLine($"Saving {uri}. Found search terms {string.Join(", ", pageResult.SearchTermsFoundInHtml.Concat(pageResult.SearchTermsFoundInText))}");
            }

            await FileHelper.SaveHtmlContentAsync(options, uri, page.HtmlContent, cancellationToken);

            pageResult.HtmlSaved = true;

            return pageResult;
        }
        catch (OperationCanceledException)
        {
            return pageResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error archiving {uri.OriginalString}. {ex.Message}");
            pageResult.IsError = true;
            return pageResult;
        }
    }

    private static bool PathIsNotExcluded(PageArchiveOptions config, Uri uri)
    {
        return !(config.ExcludedUrls?.Exists(x =>
        {
            if (x.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                x = x[..^1];

                if (x.StartsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    x = x[1..];

                    return uri.PathAndQuery.Contains(x, StringComparison.OrdinalIgnoreCase);
                }

                return uri.PathAndQuery.StartsWith(x, StringComparison.OrdinalIgnoreCase);
            }

            return uri.PathAndQuery.EqualsIgnoreCase(x);
        }) ?? false);
    }
}
