using System.Security.Cryptography;

namespace LoadTest;

public static class LoadTester
{
    public static async Task<int> RunLoadTest(LoadTesterOptions options)
    {
        var urls = options.Mode switch
        {
            LoadTesterMode.Sitemap => await SitemapHelpers.GetUrlsFromSitemapUrl(options.TargetList, options.IsVerbose),
            LoadTesterMode.UrlList => await SitemapHelpers.GetUrlsFromUrlListFile(options.TargetList),
            _ => throw new ArgumentException("Mode is not valid."),
        };

        if (urls.Length == 0)
        {
            throw new InvalidOperationException("No URLs found. Exiting.");
        }

        if (!string.IsNullOrWhiteSpace(options.MakeUrlList))
        {
            Console.WriteLine($"Writing URLs to {options.MakeUrlList}.");
            await File.WriteAllLinesAsync(options.MakeUrlList, urls);
            return 0;
        }

        Console.WriteLine($"Running load test. Press Ctrl+C to stop.\n");

        var metrics = new Metrics();
        metrics.Stopwatch.Start();

        var urlBlockSize = Convert.ToInt32(Math.Floor((double)urls.Length / options.ThreadCount));

        try
        {
            var tasks = Enumerable
                .Range(0, options.ThreadCount)
                .Select(i => StartThread(i, urls, metrics, options, urlBlockSize))
                .ToArray();

            Task.WaitAll(tasks);
        }
        finally
        {
            metrics.Stopwatch.Stop();
            Console.WriteLine($"\n{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / (metrics.Stopwatch.ElapsedMilliseconds / 1000)} RPS");

            var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
            Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");
        }

        return 0;
    }

    private static async Task StartThread(int threadNumber, string[] urls, Metrics metrics, LoadTesterOptions options, int urlBlockSize)
    {
        using var client = new HttpClient();

        // Thread number is zero-based
        var initialUrlIndex = urlBlockSize * threadNumber;

        // If the last thread, then go to the last URL, else go to the end of this block.
        var stopUrlIndex = threadNumber == options.ThreadCount ? urls.Length - 1 : urlBlockSize * (threadNumber + 1) - 1;

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        while (true)
        {
            var appendedUrl = string.Empty;

            if (options.ChanceOf404 >= 100 || (options.ChanceOf404 > 0 && RandomNumberGenerator.GetInt32(0, 100) < options.ChanceOf404))
            {
                appendedUrl = Guid.NewGuid().ToString();
            }

            var url = urls[urlIndex] + appendedUrl;

            var response = await client.GetAsync(urls[urlIndex] + appendedUrl);

            Interlocked.Increment(ref metrics.RequestCount);

            var unintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound && appendedUrl == string.Empty;

            if (unintendedMiss)
            {
                Interlocked.Increment(ref metrics.MissedRequestCount);
            }

            if (options.IsVerbose || unintendedMiss)
            {
                Console.WriteLine($"{response.StatusCode} {url}");
            }

            var shouldStopForSeconds = !options.IsAllOnce && metrics.Stopwatch.ElapsedMilliseconds >= options.SecondsToRun * 1000;
            var shouldStopForAllOnce = options.IsAllOnce && urlIndex == stopUrlIndex;

            if (shouldStopForSeconds || shouldStopForAllOnce)
            {
                break;
            }

            urlIndex = (urlIndex + 1) % urls.Length;

            if (options.IsSlowEnabled)
            {
                await Task.Delay(500);
            }
        }
    }
}
