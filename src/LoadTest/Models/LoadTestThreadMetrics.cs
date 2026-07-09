namespace LoadTest.Models;

public struct LoadTestThreadMetrics
{
    public long RequestCount { get; set; }

    public long MissedRequestCount { get; set; }

    public long ResourceRequestCount { get; set; }

    public long ResourceErrorCount { get; set; }
}
