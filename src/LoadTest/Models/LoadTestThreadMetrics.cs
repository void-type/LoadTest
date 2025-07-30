namespace LoadTest.Models;

public struct LoadTestThreadMetrics
{
    public long RequestCount { get; set; }

    public long MissedRequestCount { get; set; }
}
