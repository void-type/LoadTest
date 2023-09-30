using LoadTest.Helpers;
using System.Diagnostics;

namespace LoadTest.Services;

public static class PageArchiver
{
    /// <summary>
    /// Saves a shallow copy of the page HTML.
    /// </summary>
    public static async Task<int> ShallowArchiveAsync(PageArchiverConfiguration config, string[] urls)
    {
        if (urls.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return 1;
        }

        Console.WriteLine("Performing shallow archive. Press Ctrl+C to stop.");

        var startTime = Stopwatch.GetTimestamp();

        using var client = new HttpClient();

        var tasks = Enumerable
            .Range(0, config.ThreadCount)
            .Select(i => StartThread(i, urls, config, client))
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

    private static async Task<LoadTesterThreadMetrics> StartThread(int threadNumber, string[] urls, PageArchiverConfiguration config, HttpClient client)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, config.ThreadCount, urls.Length);

        var metrics = new LoadTesterThreadMetrics();

        if (initialUrlIndex == -1)
        {
            return metrics;
        }

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        while (true)
        {
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
                    var response = await client.GetAsync(url);

                    metrics.RequestCount++;

                    response.EnsureSuccessStatusCode();

                    if (config.IsVerbose)
                    {
                        Console.WriteLine($"{response.StatusCode} {url}");
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    if (config.IsVerbose)
                    {
                        Console.WriteLine($"Writing {content.Length} chars to {filePath}");
                    }

                    Directory.CreateDirectory(folderPath);

                    await File.WriteAllTextAsync(filePath, content);
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
                await Task.Delay(500);
            }
        }

        return metrics;
    }
}
