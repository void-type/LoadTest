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
        var urls = await _urlsRetriever.GetUrlsAsync(options.SitemapUrl, cancellationToken);

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

        Console.WriteLine($"{metrics.RequestCount} requests in {elapsedTime} = {metrics.RequestCount / safeSeconds:F2} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");
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

                var request = new HttpRequestMessage(options.RequestMethod, url);
                var response = await httpClient.SendAsync(request, cancellationToken);

                metrics.RequestCount++;

                var isUnintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound && !shouldForce404;

                if (isUnintendedMiss)
                {
                    metrics.MissedRequestCount++;
                }

                if (options.IsVerbose || isUnintendedMiss)
                {
                    Console.WriteLine($"{response.StatusCode} {url}");
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
