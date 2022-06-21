using System.Security.Cryptography;

namespace LoadTest.Services;

public static class LoadTester
{
    /// <summary>
    /// Request URLs and log metrics.
    /// </summary>
    public static int RunLoadTest(LoadTesterConfiguration config, string[] urls)
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

    private static async Task StartThread(int threadNumber, string[] urls, LoadTesterMetrics metrics, LoadTesterConfiguration config, int urlBlockSize)
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
            var appendedUrl = string.Empty;

            if (config.ChanceOf404 >= 100 || config.ChanceOf404 > 0 && RandomNumberGenerator.GetInt32(0, 100) < config.ChanceOf404)
            {
                appendedUrl = Guid.NewGuid().ToString();
            }

            var url = urls[urlIndex] + appendedUrl;

            var response = await client.GetAsync(url);

            Interlocked.Increment(ref metrics.RequestCount);

            var unintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound && appendedUrl == string.Empty;

            if (unintendedMiss)
            {
                Interlocked.Increment(ref metrics.MissedRequestCount);
            }

            if (config.IsVerbose || unintendedMiss)
            {
                Console.WriteLine($"{response.StatusCode} {url}");
            }

            var allOnceMode = config.SecondsToRun < 1;

            var shouldStopForSeconds = !allOnceMode && metrics.Stopwatch.ElapsedMilliseconds >= config.SecondsToRun * 1000;
            var shouldStopForAllOnce = allOnceMode && urlIndex == stopUrlIndex;

            if (shouldStopForSeconds || shouldStopForAllOnce)
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
