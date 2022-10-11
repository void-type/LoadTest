using LoadTest.Helpers;

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

        var metrics = new LoadTesterMetrics();
        metrics.Stopwatch.Start();

        using var client = new HttpClient();

        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = config.ThreadCount };

        await Parallel.ForEachAsync(urls, parallelOptions, async (url, _) => await ArchiveUrl(url, metrics, config, client));

        metrics.Stopwatch.Stop();
        Console.WriteLine("Finished.");

        var seconds = metrics.Stopwatch.ElapsedMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;
        Console.WriteLine($"{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / safeSeconds} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} errors = {missedPercent:F2}%");

        return 0;
    }

    private static async Task ArchiveUrl(string url, LoadTesterMetrics metrics, PageArchiverConfiguration config, HttpClient client)
    {
        var uri = new Uri(url);
        var uriSegments = uri.Segments.ToList();

        var baseFolder = config.OutputPath.TrimEnd('/', '\\');

        var lastUriSegment = uriSegments.Last();

        var fileName = (lastUriSegment == "/" ? "index" : lastUriSegment) + ".html";

        uriSegments.RemoveAt(uriSegments.Count - 1);
        var folderPath = baseFolder + string.Concat(uriSegments);

        var filePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(filePath))
        {
            try
            {
                var response = await client.GetAsync(url);

                Interlocked.Increment(ref metrics.RequestCount);

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

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                await File.WriteAllTextAsync(filePath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving {url}\n{ex}");
                Interlocked.Increment(ref metrics.MissedRequestCount);
            }
        }

        if (config.IsDelayEnabled)
        {
            await Task.Delay(500);
        }
    }
}
