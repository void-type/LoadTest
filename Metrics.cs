using System.Diagnostics;

namespace LoadTest;

public class Metrics
{
    public long RequestCount;
    public long MissedRequestCount;
    public Stopwatch Stopwatch { get; } = new();
}
