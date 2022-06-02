using System.Security.Cryptography;
using System.Xml.Linq;

namespace LoadTest;

public static class LoadTestHelpers
{
    public static async Task RunLoadTest(string mode, string targetList, int threadCount, int secondsToRun, int chanceOf404, bool isSlowEnabled)
    {
        var urls = mode switch
        {
            "sitemap-url" => await GetUrlsFromSitemapUrl(targetList),
            "sitemap-file" => GetUrlsFromSitemapFile(targetList),
            "url-list-file" => await GetUrlsFromUrlListFile(targetList),
            _ => throw new ArgumentException("Mode is not valid."),
        };

        Console.WriteLine($"Running load test. Press Ctrl+C to stop.");
        // Console.WriteLine("\n" + string.Join("\n", urls) + "\n");

        var metrics = new Metrics
        {
            ThreadCount = threadCount,
            SecondsToRun = secondsToRun,
            ChanceOf404 = chanceOf404,
            IsSlowEnabled = isSlowEnabled,
        };

        metrics.Stopwatch.Start();

        try
        {
            var tasks = Enumerable
                .Range(0, threadCount)
                .Select(i => StartThread(i, urls, metrics))
                .ToArray();

            Task.WaitAll(tasks);
        }
        finally
        {
            metrics.Stopwatch.Stop();
            Console.WriteLine($"{metrics.RequestCount} requests in {metrics.Stopwatch.Elapsed} = {metrics.RequestCount / (metrics.Stopwatch.ElapsedMilliseconds / 1000)} RPS");

            // TODO: Show unexpected 404 count?
            var missedCount = metrics.ActualMissedRequestCount - metrics.IntendedMissedRequestCount;
            var missedPercent = (double)(missedCount) / metrics.RequestCount * 100;
            Console.WriteLine($"{missedCount} unintended missed requests = {missedPercent:F2}%");
        }
    }

    private static async Task<string[]> GetUrlsFromSitemapUrl(string sitemapUrl)
    {
        using var sitemapClient = new HttpClient();
        var xmlString = await sitemapClient.GetStringAsync(sitemapUrl);
        return GetUrlsFromSitemap(XElement.Parse(xmlString));
    }

    private static string[] GetUrlsFromSitemapFile(string sitemapFilePath)
    {
        return GetUrlsFromSitemap(XElement.Load(sitemapFilePath));
    }

    private static async Task<string[]> GetUrlsFromUrlListFile(string targetList)
    {
        return await File.ReadAllLinesAsync(targetList);
    }

    private static string[] GetUrlsFromSitemap(XElement xml)
    {
        var locs = xml.Descendants()
            .Where(x => x.Name.LocalName == "loc")
            .Select(x => x.Value);

        var alts = xml.Descendants()
            .Where(x => x.Name.LocalName == "link" && x.Attribute("rel")?.Value == "alternate" && !string.IsNullOrWhiteSpace(x.Attribute("href")?.Value))
            .Select(x => x.Attribute("href")!.Value);

        // TODO: follow sitemap nodes to other sitemaps.

        return locs
            .Concat(alts)
            .Distinct()
            .ToArray();
    }

    private static async Task StartThread(int threadNumber, string[] urls, Metrics metrics)
    {
        using var client = new HttpClient();

        // Start in a different spot per-thread.
        var urlIndex = Convert.ToInt32(Math.Floor((double)urls.Length / metrics.ThreadCount) * threadNumber);

        while (true)
        {
            var appendedUrl = string.Empty;

            if (metrics.ChanceOf404 > 0)
            {
                var roll = RandomNumberGenerator.GetInt32(0, 100);

                if (roll < metrics.ChanceOf404)
                {
                    Interlocked.Increment(ref metrics.IntendedMissedRequestCount);
                    appendedUrl = Guid.NewGuid().ToString();
                }
            }

            Console.WriteLine(urls[urlIndex] + appendedUrl);
            var response = await client.GetAsync(urls[urlIndex] + appendedUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Interlocked.Increment(ref metrics.ActualMissedRequestCount);
            }

            urlIndex = (urlIndex + 1) % urls.Length;

            Interlocked.Increment(ref metrics.RequestCount);

            if (metrics.Stopwatch.ElapsedMilliseconds >= metrics.SecondsToRun * 1000)
            {
                break;
            }

            if (metrics.IsSlowEnabled)
            {
                await Task.Delay(500);
            }
        }
    }
}
