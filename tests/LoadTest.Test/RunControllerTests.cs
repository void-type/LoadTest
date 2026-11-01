namespace LoadTest.Test;

using LoadTest.Helpers;

public class RunControllerTests
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void Starts_running()
    {
        var run = new RunController();

        Assert.False(run.IsPaused);
        Assert.False(run.IsQuitRequested);
        Assert.Equal(TimeSpan.Zero, run.PausedTime);
    }

    [Fact]
    public void Pause_and_resume_report_whether_state_changed()
    {
        var run = new RunController();

        Assert.False(run.Resume());
        Assert.True(run.Pause());
        Assert.True(run.IsPaused);
        Assert.False(run.Pause());
        Assert.True(run.Resume());
        Assert.False(run.IsPaused);
        Assert.False(run.Resume());
    }

    [Fact]
    public void Quit_reports_whether_state_changed()
    {
        var run = new RunController();

        Assert.True(run.Quit());
        Assert.True(run.IsQuitRequested);
        Assert.False(run.Quit());
    }

    [Fact]
    public void Quit_clears_a_pause_and_blocks_new_ones()
    {
        var run = new RunController();
        run.Pause();

        run.Quit();

        Assert.False(run.IsPaused);
        Assert.False(run.Pause());
    }

    [Fact]
    public async Task WaitToContinue_completes_immediately_when_running()
    {
        Assert.True(await new RunController().WaitToContinueAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitToContinue_blocks_while_paused_until_resumed()
    {
        var run = new RunController();
        run.Pause();

        var wait = run.WaitToContinueAsync(CancellationToken.None);

        await Task.Delay(Short);
        Assert.False(wait.IsCompleted);

        run.Resume();

        Assert.True(await wait.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitToContinue_releases_every_waiter_on_resume()
    {
        var run = new RunController();
        run.Pause();

        var waits = Enumerable.Range(0, 5).Select(_ => run.WaitToContinueAsync(CancellationToken.None)).ToArray();

        run.Resume();

        Assert.All(await Task.WhenAll(waits).WaitAsync(TimeSpan.FromSeconds(5)), Assert.True);
    }

    [Fact]
    public async Task WaitToContinue_blocks_again_after_a_second_pause()
    {
        var run = new RunController();
        run.Pause();
        run.Resume();
        run.Pause();

        var wait = run.WaitToContinueAsync(CancellationToken.None);

        await Task.Delay(Short);
        Assert.False(wait.IsCompleted);

        run.Resume();
        Assert.True(await wait.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitToContinue_returns_false_when_quit_while_paused()
    {
        var run = new RunController();
        run.Pause();

        var wait = run.WaitToContinueAsync(CancellationToken.None);
        run.Quit();

        Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitToContinue_returns_false_when_already_quit()
    {
        var run = new RunController();
        run.Quit();

        Assert.False(await run.WaitToContinueAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitToContinue_throws_when_cancelled_while_paused()
    {
        var run = new RunController();
        run.Pause();
        using var cts = new CancellationTokenSource();

        var wait = run.WaitToContinueAsync(cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void PausedTime_includes_a_pause_in_progress_and_accumulates_across_pauses()
    {
        var clock = new ManualTimeProvider();
        var run = new RunController(clock);

        run.Pause();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.FromSeconds(10), run.PausedTime);

        run.Resume();
        clock.Advance(TimeSpan.FromSeconds(100));
        Assert.Equal(TimeSpan.FromSeconds(10), run.PausedTime);

        run.Pause();
        clock.Advance(TimeSpan.FromSeconds(5));
        run.Resume();

        Assert.Equal(TimeSpan.FromSeconds(15), run.PausedTime);
    }

    [Fact]
    public void Quitting_while_paused_stops_counting_paused_time()
    {
        var clock = new ManualTimeProvider();
        var run = new RunController(clock);

        run.Pause();
        clock.Advance(TimeSpan.FromSeconds(10));
        run.Quit();
        clock.Advance(TimeSpan.FromSeconds(100));

        Assert.Equal(TimeSpan.FromSeconds(10), run.PausedTime);
    }

    [Fact]
    public void GetActiveElapsed_excludes_paused_time()
    {
        var clock = new ManualTimeProvider();
        var run = new RunController(clock);
        var start = run.GetTimestamp();

        clock.Advance(TimeSpan.FromSeconds(3));
        run.Pause();
        clock.Advance(TimeSpan.FromSeconds(60));
        Assert.Equal(TimeSpan.FromSeconds(3), run.GetActiveElapsed(start));

        run.Resume();
        clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(5), run.GetActiveElapsed(start));
    }

    [Fact]
    public void GetActiveElapsed_is_never_negative()
    {
        var clock = new ManualTimeProvider();
        var run = new RunController(clock);

        // Paused before the start timestamp was taken.
        run.Pause();
        clock.Advance(TimeSpan.FromSeconds(10));
        var start = run.GetTimestamp();

        Assert.Equal(TimeSpan.Zero, run.GetActiveElapsed(start));
    }
}
