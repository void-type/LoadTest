using LoadTest.Helpers;
using LoadTest.Models;
using System.Diagnostics;
using System.Security.Cryptography;

namespace LoadTest.Services;

public class LoadTester
{
    private readonly HttpClient _httpClient;
    private readonly UrlsRetriever _urlsRetriever;

    public LoadTester(HttpClient httpClient, UrlsRetriever urlsRetriever)
    {
        _httpClient = httpClient;
        _urlsRetriever = urlsRetriever;
    }

    /// <summary>
    /// Request URLs and log metrics.
    /// </summary>
    public async Task RunLoadTestAsync(LoadTestOptions options, CancellationToken cancellationToken)
    {
        var urls = await _urlsRetriever.GetUrlsAsync(options.SitemapUrl, options.CustomHeaders, options.UserAgent, cancellationToken);

        if (urls.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return;
        }

        Console.WriteLine("Running load test. Press Ctrl+C to stop.");

        var startTime = Stopwatch.GetTimestamp();

        var tasks = Enumerable
            .Range(0, options.ThreadCount)
            .Select(i => StartThreadAsync(i, urls, startTime, options, _httpClient, cancellationToken))
            .ToArray();

        var metricCollection = await Task.WhenAll(tasks);

        var metrics = metricCollection.Aggregate(new LoadTestThreadMetrics(), (acc, x) =>
        {
            acc.RequestCount += x.RequestCount;
            acc.MissedRequestCount += x.MissedRequestCount;
            acc.ResourceRequestCount += x.ResourceRequestCount;
            acc.ResourceErrorCount += x.ResourceErrorCount;
            return acc;
        });

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

        Console.WriteLine($"{metrics.RequestCount} pages in {elapsedTime} = {metrics.RequestCount / safeSeconds:F2} PPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        if (options.IncludeResources)
        {
            var resourceErrorPercent = metrics.ResourceRequestCount > 0
                ? (double)metrics.ResourceErrorCount / metrics.ResourceRequestCount * 100
                : 0;
            Console.WriteLine($"{metrics.ResourceRequestCount} resource requests, {metrics.ResourceErrorCount} resource errors = {resourceErrorPercent:F2}%");
        }
    }

    private static async Task<LoadTestThreadMetrics> StartThreadAsync(int threadNumber, string[] urls, long startTime,
        LoadTestOptions options, HttpClient httpClient, CancellationToken cancellationToken)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, options.ThreadCount, urls.Length);

        var metrics = new LoadTestThreadMetrics();

        if (initialUrlIndex == -1)
        {
            return metrics;
        }

        // Defines if we hit all URLs in the list once or if we run until time limit.
        var shouldHitAllUrlsOnce = options.SecondsToRun < 1;

        var urlIndex = initialUrlIndex;

        async Task RequestPageResourcesAsync(HttpResponseMessage pageResponse, string pageUrl)
        {
            try
            {
                var html = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
                var pageUri = pageResponse.RequestMessage?.RequestUri ?? new Uri(pageUrl);
                var resourceLinks = await HtmlScanner.FindResourceLinksAsync(pageUri, html, options.ResourceDomains, cancellationToken);

                foreach (var resourceUri in resourceLinks)
                {
                    metrics.ResourceRequestCount++;

                    try
                    {
                        var resourceRequest = new HttpRequestMessage(HttpMethod.Get, resourceUri);
                        HttpRequestHelper.ApplyHeaders(resourceRequest, options.CustomHeaders, options.UserAgent);
                        using var resourceResponse = await httpClient.SendAsync(resourceRequest, cancellationToken);

                        if (!resourceResponse.IsSuccessStatusCode())
                        {
                            metrics.ResourceErrorCount++;
                        }

                        if (options.IsVerbose)
                        {
                            Console.WriteLine($"{resourceResponse.StatusCode} {resourceUri}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // A request that never completed (timeout, DNS failure, connection refused) is still a resource error.
                        metrics.ResourceErrorCount++;
                        Console.WriteLine($"Error requesting resource {resourceUri}: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning {pageUrl} for resources: {ex.Message}");
            }
        }

        try
        {
            while (true)
            {
                var url = urls[urlIndex];

                var shouldForce404 = options.ChanceOf404 >= 100 || (options.ChanceOf404 > 0
                    && RandomNumberGenerator.GetInt32(0, 100) < options.ChanceOf404);

                if (shouldForce404)
                {
                    url += Guid.NewGuid().ToString();
                }

                metrics.RequestCount++;

                try
                {
                    var request = new HttpRequestMessage(options.RequestMethod, url);
                    HttpRequestHelper.ApplyHeaders(request, options.CustomHeaders, options.UserAgent);
                    using var response = await httpClient.SendAsync(request, cancellationToken);

                    var isUnintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound && !shouldForce404;

                    if (isUnintendedMiss)
                    {
                        metrics.MissedRequestCount++;
                    }

                    if (options.IsVerbose || isUnintendedMiss)
                    {
                        Console.WriteLine($"{response.StatusCode} {url}");
                    }

                    if (options.IncludeResources)
                    {
                        var mediaType = response.Content.Headers.ContentType?.MediaType;
                        var isHtml = mediaType is not null &&
                            (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
                                mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase));

                        if (isHtml)
                        {
                            await RequestPageResourcesAsync(response, url);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // A request that never completed (timeout, DNS failure, connection reset) is an unintended miss
                    // rather than a reason to abort the whole run.
                    metrics.MissedRequestCount++;
                    Console.WriteLine($"Error requesting {url}: {ex.Message}");
                }

                if (shouldHitAllUrlsOnce)
                {
                    if (urlIndex == stopUrlIndex)
                    {
                        // Stop because we hit all the URLs once.
                        break;
                    }
                }
                else if (Stopwatch.GetElapsedTime(startTime).TotalMilliseconds >= options.SecondsToRun * 1000)
                {
                    // Stop because time limit.
                    break;
                }

                // Get the next URL, looping around to beginning if at the end.
                urlIndex = (urlIndex + 1) % urls.Length;

                if (options.IsDelayEnabled)
                {
                    // Delay is per-page, not per-request. A page and its resources simulate a single
                    // user loading a page (all resources fetched at once), then pausing before the next.
                    await Task.Delay(500, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }

        if (options.IsVerbose)
        {
            Console.WriteLine($"Thread {threadNumber} ending.");
        }

        return metrics;
    }
}
