namespace LoadTest.Test;

using LoadTest.Helpers;
using LoadTest.Models;
using LoadTest.Services;

public sealed class LoadTesterPauseTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(300);

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "LoadTestTests", Guid.NewGuid().ToString("N"));
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly LoadTestOptions _options;
    private readonly LoadTester _loadTester;

    public LoadTesterPauseTests()
    {
        Directory.CreateDirectory(_tempFolder);

        var urls = Enumerable.Range(1, 5).Select(i => $"https://example.com/page{i}").ToArray();
        var urlsPath = Path.Combine(_tempFolder, "urls.txt");
        File.WriteAllLines(urlsPath, urls);

        foreach (var url in urls)
        {
            _handler.AddHtml(url, "<html><body>hi</body></html>");
        }

        // One thread, one pass over the list.
        _options = new LoadTestOptions { SitemapUrl = urlsPath, ThreadCount = 1, SecondsToRun = 0 };

        var httpClient = _handler.CreateClient();
        _loadTester = new LoadTester(httpClient, new UrlsRetriever(httpClient));
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, recursive: true);
    }

    private void OnFirstRequest(Action action)
    {
        var calls = 0;

        _handler.OnRequest = _ =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                action();
            }
        };
    }

    [Fact]
    public async Task Requests_every_url_once_when_not_interrupted()
    {
        await _loadTester.RunLoadTestAsync(_options, CancellationToken.None, new RunController()).WaitAsync(Timeout);

        Assert.Equal(5, _handler.Requests.Count);
    }

    [Fact]
    public async Task Makes_no_requests_when_quit_before_starting()
    {
        var run = new RunController();
        run.Quit();

        await _loadTester.RunLoadTestAsync(_options, CancellationToken.None, run).WaitAsync(Timeout);

        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Quit_stops_before_the_next_request()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Quit());

        await _loadTester.RunLoadTestAsync(_options, CancellationToken.None, run).WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Pause_holds_requests_until_resumed()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Pause());

        var task = _loadTester.RunLoadTestAsync(_options, CancellationToken.None, run);

        await Task.Delay(Settle);

        Assert.Single(_handler.Requests);
        Assert.False(task.IsCompleted);

        run.Resume();
        await task.WaitAsync(Timeout);

        Assert.Equal(5, _handler.Requests.Count);
    }

    [Fact]
    public async Task Quit_while_paused_ends_the_run()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Pause());

        var task = _loadTester.RunLoadTestAsync(_options, CancellationToken.None, run);

        await Task.Delay(Settle);
        run.Quit();
        await task.WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Cancelling_while_paused_ends_the_run()
    {
        var run = new RunController();
        using var cts = new CancellationTokenSource();
        OnFirstRequest(() => run.Pause());

        var task = _loadTester.RunLoadTestAsync(_options, cts.Token, run);

        await Task.Delay(Settle);
        await cts.CancelAsync();
        await task.WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Pause_stops_all_threads()
    {
        var run = new RunController();
        var options = new LoadTestOptions { SitemapUrl = _options.SitemapUrl, ThreadCount = 5, SecondsToRun = 0 };
        run.Pause();

        var task = _loadTester.RunLoadTestAsync(options, CancellationToken.None, run);

        await Task.Delay(Settle);
        Assert.Empty(_handler.Requests);

        run.Resume();
        await task.WaitAsync(Timeout);

        Assert.Equal(5, _handler.Requests.Count);
    }
}
