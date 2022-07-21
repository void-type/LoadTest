using LoadTest.Helpers;

namespace LoadTest.Services;

public static class PageArchiver
{
    /// <summary>
    /// Saves a shallow copy of the page HTML.
    /// </summary>
    public static int ShallowArchive(PageArchiverConfiguration config, string[] urls)
    {
        if (urls.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return 1;
        }

        Console.WriteLine("Performing shallow archive. Press Ctrl+C to stop.");

        var metrics = new LoadTesterMetrics();
        metrics.Stopwatch.Start();

        var tasks = Enumerable
            .Range(0, config.ThreadCount)
            .Select(i => StartThread(i, urls, metrics, config))
            .ToArray();

        Task.WaitAll(tasks);

        metrics.Stopwatch.Stop();
        Console.WriteLine("Finished.");

        var seconds = metrics.Stopwatch.ElapsedMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;
        Console.WriteLine($"{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / seconds} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} errors = {missedPercent:F2}%");

        return 0;
    }

    private static async Task StartThread(int threadNumber, string[] urls, LoadTesterMetrics metrics, PageArchiverConfiguration config)
    {
        using var client = new HttpClient();

        (int initialUrlIndex, int stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, config.ThreadCount, urls.Length);

        if (initialUrlIndex == -1)
        {
            return;
        }

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        while (true)
        {
            var url = urls[urlIndex];

            var uri = new Uri(url);
            var folderPathSegments = uri.Segments.ToList();
            var fileName = folderPathSegments.Last() + ".html";
            folderPathSegments.RemoveAt(folderPathSegments.Count - 1);
            var folderPath = config.OutputPath.TrimEnd('/', '\\') + string.Join(string.Empty, folderPathSegments);
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

            var shouldStop = urlIndex == stopUrlIndex;

            if (shouldStop)
            {
                break;
            }

            urlIndex = (urlIndex + 1) % urls.Length;

            if (config.IsDelayEnabled)
            {
                await Task.Delay(500);
            }
        }
    }
}
