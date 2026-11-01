namespace LoadTest.Helpers;

/// <summary>
/// Listens for pause, resume, and stop keys in the background and applies them to a <see cref="RunController"/>.
/// </summary>
public sealed class KeyListener : IAsyncDisposable
{
    private const string KeyHint = "Press 'p' to pause, 'r' to resume, 'q' to stop and show results, or Ctrl+C to cancel.";
    private const string CancelHint = "Press Ctrl+C to stop.";

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _listener;

    private KeyListener(RunController run)
    {
        _listener = Task.Run(() => ListenAsync(run, _cts.Token), CancellationToken.None);
    }

    /// <summary>
    /// Starts listening. Returns null when there is no keyboard to listen to, such as when input is redirected.
    /// </summary>
    public static KeyListener? Start(RunController run)
    {
        return Console.IsInputRedirected ? null : new KeyListener(run);
    }

    /// <summary>
    /// The instructions to show the user, which depend on whether keys are being listened to.
    /// </summary>
    public static string GetHint(KeyListener? keyListener) => keyListener is null ? CancelHint : KeyHint;

    /// <summary>
    /// Applies a key press to the controller.
    /// </summary>
    /// <returns>A message to show the user, or null if the key did nothing.</returns>
    public static string? HandleKey(RunController run, char key)
    {
        return char.ToLowerInvariant(key) switch
        {
            'p' when run.Pause() => "Paused. Finishing in-flight requests. Press 'r' to resume or 'q' to stop.",
            'r' when run.Resume() => "Resumed.",
            'q' when run.Quit() => "Stopping after in-flight requests finish.",
            _ => null,
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _listener;
        _cts.Dispose();
    }

    private static async Task ListenAsync(RunController run, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                while (Console.KeyAvailable)
                {
                    var message = HandleKey(run, Console.ReadKey(intercept: true).KeyChar);

                    if (message is not null)
                    {
                        Console.WriteLine(message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the run ends.
        }
    }
}
