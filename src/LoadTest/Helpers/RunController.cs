namespace LoadTest.Helpers;

/// <summary>
/// Lets a long-running command be paused, resumed, or stopped. Worker threads check in between pages,
/// so requests already in flight finish before a pause takes effect.
/// </summary>
public sealed class RunController(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource _quitSignal = NewSignal();
    private TaskCompletionSource _resumedSignal = NewSignal(completed: true);
    private bool _isPaused;
    private long _pausedAt;
    private TimeSpan _pausedTotal;

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                return _isPaused;
            }
        }
    }

    public bool IsQuitRequested => _quitSignal.Task.IsCompleted;

    /// <summary>
    /// Total time spent paused, including a pause still in progress.
    /// </summary>
    public TimeSpan PausedTime
    {
        get
        {
            lock (_lock)
            {
                return _isPaused
                    ? _pausedTotal + _timeProvider.GetElapsedTime(_pausedAt)
                    : _pausedTotal;
            }
        }
    }

    /// <summary>
    /// Requests a pause. Returns true if the state changed. Has no effect once a quit is requested.
    /// </summary>
    public bool Pause()
    {
        lock (_lock)
        {
            if (_isPaused || IsQuitRequested)
            {
                return false;
            }

            _isPaused = true;
            _pausedAt = _timeProvider.GetTimestamp();
            _resumedSignal = NewSignal();
            return true;
        }
    }

    /// <summary>
    /// Lifts a pause. Returns true if the state changed.
    /// </summary>
    public bool Resume()
    {
        lock (_lock)
        {
            if (!_isPaused)
            {
                return false;
            }

            EndPause();
            return true;
        }
    }

    /// <summary>
    /// Requests that the run stop. Wakes anything waiting on a pause. Returns true if the state changed.
    /// </summary>
    public bool Quit()
    {
        lock (_lock)
        {
            if (!_quitSignal.TrySetResult())
            {
                return false;
            }

            if (_isPaused)
            {
                EndPause();
            }

            return true;
        }
    }

    /// <summary>
    /// Completes immediately when not paused, otherwise when resumed or when a quit is requested.
    /// </summary>
    /// <returns>True if the run should continue, false if a quit was requested.</returns>
    public async Task<bool> WaitToContinueAsync(CancellationToken cancellationToken)
    {
        Task resumed;

        lock (_lock)
        {
            resumed = _resumedSignal.Task;
        }

        await resumed.WaitAsync(cancellationToken);

        return !IsQuitRequested;
    }

    /// <summary>
    /// Time elapsed since <paramref name="startTimestamp"/> (from <see cref="TimeProvider.GetTimestamp"/>), not counting time spent paused.
    /// Assumes the run was not paused before the start timestamp.
    /// </summary>
    public TimeSpan GetActiveElapsed(long startTimestamp)
    {
        var active = _timeProvider.GetElapsedTime(startTimestamp) - PausedTime;

        return active > TimeSpan.Zero ? active : TimeSpan.Zero;
    }

    /// <summary>
    /// Gets a timestamp from the same clock used for <see cref="GetActiveElapsed"/>.
    /// </summary>
    public long GetTimestamp() => _timeProvider.GetTimestamp();

    // Must be called while holding the lock.
    private void EndPause()
    {
        _pausedTotal += _timeProvider.GetElapsedTime(_pausedAt);
        _isPaused = false;
        _resumedSignal.SetResult();
    }

    private static TaskCompletionSource NewSignal(bool completed = false)
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (completed)
        {
            signal.SetResult();
        }

        return signal;
    }
}
