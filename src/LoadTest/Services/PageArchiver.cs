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

        Console.WriteLine("Running load test. Press Ctrl+C to stop.");

        var metrics = new LoadTesterMetrics();
        metrics.Stopwatch.Start();

        var urlBlockSize = Convert.ToInt32(Math.Floor((double)urls.Length / config.ThreadCount));

        var tasks = Enumerable
            .Range(0, config.ThreadCount)
            .Select(i => StartThread(i, urls, metrics, config, urlBlockSize))
            .ToArray();

        Task.WaitAll(tasks);

        metrics.Stopwatch.Stop();
        Console.WriteLine("Finished.");
        Console.WriteLine($"{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / (metrics.Stopwatch.ElapsedMilliseconds / 1000)} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        return 0;
    }

    private static async Task StartThread(int threadNumber, string[] urls, LoadTesterMetrics metrics, PageArchiverConfiguration config, int urlBlockSize)
    {
        using var client = new HttpClient();

        // Thread number is zero-based
        var initialUrlIndex = urlBlockSize * threadNumber;

        // If the last thread, then go to the last URL, else go to the end of this block.
        var stopUrlIndex = threadNumber == config.ThreadCount ? urls.Length - 1 : urlBlockSize * (threadNumber + 1) - 1;

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        while (true)
        {
            var url = urls[urlIndex];

            var uri = new Uri(url);
            var folderPathSegments = uri.Segments.ToList();
            var fileName = folderPathSegments.Last();
            folderPathSegments.RemoveAt(folderPathSegments.Count - 1);
            var folderPath = config.OutputPath.TrimEnd('/', '\\') + string.Join(string.Empty, folderPathSegments);
            var filePath = Path.Combine(folderPath, fileName) + ".html";

            if (!File.Exists(filePath))
            {
                try
                {
                    var response = await client.GetAsync(url);

                    Interlocked.Increment(ref metrics.RequestCount);

                    var unintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound;

                    if (unintendedMiss)
                    {
                        Interlocked.Increment(ref metrics.MissedRequestCount);
                    }

                    if (config.IsVerbose || unintendedMiss)
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
                    Console.WriteLine($"Error getting {url}\n{ex}.");
                }
            }

            var shouldStop = urlIndex == stopUrlIndex;

            urlIndex = (urlIndex + 1) % urls.Length;

            if (shouldStop)
            {
                break;
            }

            if (config.IsDelayEnabled)
            {
                await Task.Delay(500);
            }
        }
    }
}
