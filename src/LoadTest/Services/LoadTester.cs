using System.Security.Cryptography;
using LoadTest.Helpers;

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

        var tasks = Enumerable
            .Range(0, config.ThreadCount)
            .Select(i => StartThread(i, urls, metrics, config))
            .ToArray();

        Task.WaitAll(tasks);

        metrics.Stopwatch.Stop();
        Console.WriteLine("Finished.");

        var seconds = metrics.Stopwatch.ElapsedMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;
        Console.WriteLine($"{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / safeSeconds} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        return 0;
    }

    private static async Task StartThread(int threadNumber, string[] urls, LoadTesterMetrics metrics, LoadTesterConfiguration config)
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
