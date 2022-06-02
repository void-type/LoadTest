using System.Diagnostics;

namespace LoadTest;

public class Metrics
{
    public long RequestCount = 0;
    public long IntendedMissedRequestCount = 0;
    public long ActualMissedRequestCount = 0;
    public int ThreadCount { get; init; }
    public int SecondsToRun { get; init; }
    public int ChanceOf404 { get; init; }
    public Stopwatch Stopwatch { get; } = new();
    public bool IsSlowEnabled { get; init; }
}
