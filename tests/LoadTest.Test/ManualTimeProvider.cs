namespace LoadTest.Test;

/// <summary>
/// A clock that only moves when told to.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

    public void Advance(TimeSpan amount) => Interlocked.Add(ref _timestamp, amount.Ticks);
}
