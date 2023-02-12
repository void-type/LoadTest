namespace LoadTest.Services;

public class LoadTesterConfiguration
{
    public int ThreadCount { get; init; }
    public int SecondsToRun { get; init; }
    public int ChanceOf404 { get; init; }
    public bool IsDelayEnabled { get; init; }
    public HttpMethod RequestMethod { get; init; } = HttpMethod.Get;
    public bool IsVerbose { get; init; }
}
