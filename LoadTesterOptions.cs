namespace LoadTest;

public class LoadTesterOptions
{
    public string Mode { get; init; } = string.Empty;
    public string TargetList { get; init; } = string.Empty;
    public int ThreadCount { get; init; }
    public int SecondsToRun { get; init; }
    public int ChanceOf404 { get; init; }
    public bool IsSlowEnabled { get; init; }
    public bool IsVerbose { get; init; }
}
