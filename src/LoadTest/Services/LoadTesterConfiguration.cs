namespace LoadTest.Services;

public class LoadTesterConfiguration
{
    public string Path { get; init; } = string.Empty;
    public int ThreadCount { get; init; }
    public int SecondsToRun { get; init; }
    public int ChanceOf404 { get; init; }
    public bool IsDelayEnabled { get; init; }
    public bool IsVerbose { get; init; }
}
