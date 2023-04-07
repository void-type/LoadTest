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

        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = config.ThreadCount };

        await Parallel.ForEachAsync(urls, parallelOptions, async (url, _) => await ArchiveUrl(url, config, client));

        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine("Finished.");

        var seconds = elapsedTime.TotalMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;
        Console.WriteLine($"{urls.Length} requests in {elapsedTime} = {urls.Length / safeSeconds:F2} RPS");

        return 0;
    }

    private static async Task ArchiveUrl(string url, PageArchiverConfiguration config, HttpClient client)
    {
        var metrics = new LoadTesterThreadMetrics();

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

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                await File.WriteAllTextAsync(filePath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving {url}. {ex.Message}");
                metrics.MissedRequestCount++;
            }
        }

        if (config.IsDelayEnabled)
        {
            await Task.Delay(500);
        }
    }
}
