using LoadTest.Helpers;
using System.Diagnostics;

namespace LoadTest.Services;

public static class PageArchiver
{
    /// <summary>
    /// Saves a copy of the page HTML.
    /// </summary>
    public static async Task<int> ArchiveHtmlAsync(PageArchiverConfiguration config, string[] urls, CancellationToken cancellationToken)
    {
        try
        {
            if (urls.Length == 0)
            {
                Console.WriteLine("No URLs found. Exiting.");
                return 1;
            }

            Console.WriteLine("Performing HTML archive. Press Ctrl+C to stop.");

            var startTime = Stopwatch.GetTimestamp();

            using var client = new HtmlContentRetriever(config);
            await client.Init();

            var tasks = Enumerable
                .Range(0, config.ThreadCount)
                .Select(i => StartThreadAsync(i, urls, config, client, cancellationToken))
                .ToArray();

            var metricCollection = await Task.WhenAll(tasks);

            var metrics = metricCollection.Aggregate(new LoadTesterThreadMetrics(), (acc, x) =>
            {
                acc.RequestCount += x.RequestCount;
                acc.MissedRequestCount += x.MissedRequestCount;
                return acc;
            });

            Console.WriteLine("Finished.");

            var elapsedTime = Stopwatch.GetElapsedTime(startTime);
            var seconds = elapsedTime.TotalMilliseconds / 1000;
            var safeSeconds = seconds == 0 ? 1 : seconds;

            Console.WriteLine($"{metrics.RequestCount} requests in {elapsedTime} = {metrics.RequestCount / safeSeconds:F2} RPS");

            var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
            Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

            return 0;
        }
        catch
        {
            // This handles our cancellation requests and closes the scope to dispose of HtmlContentRetriever.
            return 1;
        }
    }

    private static async Task<LoadTesterThreadMetrics> StartThreadAsync(int threadNumber, string[] urls, PageArchiverConfiguration config, HtmlContentRetriever client, CancellationToken cancellationToken)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, config.ThreadCount, urls.Length);

        var metrics = new LoadTesterThreadMetrics()
        {
            ThreadNumber = threadNumber
        };

        if (initialUrlIndex == -1)
        {
            return metrics;
        }

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = urls[urlIndex];

            var uri = new Uri(url);

            var baseFolder = config.OutputPath.TrimEnd('/', '\\');

            var lastUriSegment = uri.Segments[^1];

            var fileName = (lastUriSegment == "/" ? "index" : lastUriSegment) + ".html";

            var folderPath = baseFolder + string.Concat(uri.Segments[..^1]);

            var filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                try
                {
                    var content = await client.GetContentAsync(url, metrics, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (config.IsVerbose)
                    {
                        Console.WriteLine($"Writing {content.Length} chars to {filePath}");
                    }

                    Directory.CreateDirectory(folderPath);

                    await File.WriteAllTextAsync(filePath, content, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error archiving {url}. {ex.Message}");
                    metrics.MissedRequestCount++;
                }
            }

            if (urlIndex == stopUrlIndex)
            {
                // Stop because we hit all the URLs once.
                break;
            }

            // Get the next URL, looping around to beginning if at the end.
            urlIndex++;

            if (config.IsDelayEnabled)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        return metrics;
    }
}
