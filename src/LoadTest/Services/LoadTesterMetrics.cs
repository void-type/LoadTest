namespace LoadTest.Services;

public struct LoadTesterThreadMetrics
{
    public int ThreadNumber { get; init; }
    public long RequestCount { get; set; }
    public long MissedRequestCount { get; set; }
}
