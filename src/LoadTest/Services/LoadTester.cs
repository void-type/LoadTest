﻿using LoadTest.Helpers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace LoadTest.Services;

public static class LoadTester
{
    /// <summary>
    /// Request URLs and log metrics.
    /// </summary>
    public static async Task<int> RunLoadTestAsync(LoadTesterConfiguration config, string[] urls, CancellationToken cancellationToken)
    {
        if (urls.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return 1;
        }

        Console.WriteLine("Running load test. Press Ctrl+C to stop.");

        var startTime = Stopwatch.GetTimestamp();

        using var client = new HttpClient();

        var tasks = Enumerable
            .Range(0, config.ThreadCount)
            .Select(i => StartThreadAsync(i, urls, startTime, config, client, cancellationToken))
            .ToArray();

        var metricCollection = await Task.WhenAll(tasks);

        var metrics = metricCollection.Aggregate(new LoadTesterThreadMetrics(), (acc, x) =>
        {
            acc.RequestCount += x.RequestCount;
            acc.MissedRequestCount += x.MissedRequestCount;
            return acc;
        });

        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Cancelled.");
        }
        else
        {
            Console.WriteLine("Finished.");
        }

        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        var seconds = elapsedTime.TotalMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;

        Console.WriteLine($"{metrics.RequestCount} requests in {elapsedTime} = {metrics.RequestCount / safeSeconds:F2} RPS");

        var missedPercent = (double)metrics.MissedRequestCount / metrics.RequestCount * 100;
        Console.WriteLine($"{metrics.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        return 0;
    }

    private static async Task<LoadTesterThreadMetrics> StartThreadAsync(int threadNumber, string[] urls, long startTime, LoadTesterConfiguration config, HttpClient client, CancellationToken cancellationToken)
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

        // Defines if we hit all URLs in the list once or if we run until time limit.
        var shouldHitAllUrlsOnce = config.SecondsToRun < 1;

        // Start in a different spot per-thread.
        var urlIndex = initialUrlIndex;

        try
        {
            while (true)
            {
                var url = urls[urlIndex];
                var shouldForce404 = config.ChanceOf404 >= 100 || (config.ChanceOf404 > 0 && RandomNumberGenerator.GetInt32(0, 100) < config.ChanceOf404);

                if (shouldForce404)
                {
                    url += Guid.NewGuid().ToString();
                }

                var request = new HttpRequestMessage(config.RequestMethod, url);
                var response = await client.SendAsync(request, cancellationToken);

                metrics.RequestCount++;

                var isUnintendedMiss = response.StatusCode == System.Net.HttpStatusCode.NotFound && !shouldForce404;

                if (isUnintendedMiss)
                {
                    metrics.MissedRequestCount++;
                }

                if (config.IsVerbose || isUnintendedMiss)
                {
                    Console.WriteLine($"{response.StatusCode} {url}");
                }

                if (shouldHitAllUrlsOnce)
                {
                    if (urlIndex == stopUrlIndex)
                    {
                        // Stop because we hit all the URLs once.
                        break;
                    }
                }
                else if (Stopwatch.GetElapsedTime(startTime).TotalMilliseconds >= config.SecondsToRun * 1000)
                {
                    // Stop because time limit.
                    break;
                }

                // Get the next URL, looping around to beginning if at the end.
                urlIndex = (urlIndex + 1) % urls.Length;

                if (config.IsDelayEnabled)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }

        if (config.IsVerbose)
        {
            Console.WriteLine($"Thread {metrics.ThreadNumber} ending.");
        }

        return metrics;
    }
}
