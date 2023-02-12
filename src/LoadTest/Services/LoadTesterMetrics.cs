namespace LoadTest.Services;

public struct LoadTesterThreadMetrics
{
    public long RequestCount { get; set; }
    public long MissedRequestCount { get; set; }
}
