﻿namespace LoadTest.Services;

public class PageArchiverConfiguration
{
    public string OutputPath { get; init; } = string.Empty;
    public int ThreadCount { get; init; }
    public bool IsDelayEnabled { get; init; }
    public bool IsVerbose { get; init; }
}
