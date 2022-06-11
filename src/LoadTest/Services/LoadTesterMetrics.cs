using System.Diagnostics;

namespace LoadTest.Services;

public class LoadTesterMetrics
{
    public long RequestCount;
    public long MissedRequestCount;
    public Stopwatch Stopwatch { get; } = new();
}
