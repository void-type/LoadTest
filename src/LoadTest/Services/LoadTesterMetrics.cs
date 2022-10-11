using System.Diagnostics;

namespace LoadTest.Services;

public class LoadTesterMetrics
{
    // These are intentionally public fields for concurrent incrementing.
#pragma warning disable S1104
    public long RequestCount;
    public long MissedRequestCount;
#pragma warning disable S1104

    public Stopwatch Stopwatch { get; } = new();
}
