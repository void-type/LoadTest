namespace LoadTest.Test;

using LoadTest.Helpers;
using LoadTest.Models;
using LoadTest.Services;

public sealed class PageArchiverPauseTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(300);

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "LoadTestTests", Guid.NewGuid().ToString("N"));
    private readonly string _outputFolder;
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly HtmlContentRetriever _retriever;
    private readonly PageArchiver _archiver;
    private readonly PageArchiveOptions _options;

    public PageArchiverPauseTests()
    {
        Directory.CreateDirectory(_tempFolder);
        _outputFolder = Path.Combine(_tempFolder, "output");

        var urls = Enumerable.Range(1, 5).Select(i => $"https://example.com/page{i}").ToArray();
        var urlsPath = Path.Combine(_tempFolder, "urls.txt");
        File.WriteAllLines(urlsPath, urls);

        foreach (var url in urls)
        {
            _handler.AddHtml(url, "<html><body>hi</body></html>");
        }

        // One thread, one pass over the list.
        _options = new PageArchiveOptions
        {
            SitemapUrl = urlsPath,
            OutputPath = _outputFolder,
            ThreadCount = 1,
            PrimaryDomain = "example.com",
        };

        var httpClient = _handler.CreateClient();
        _retriever = new HtmlContentRetriever(httpClient);
        _archiver = new PageArchiver(new UrlsRetriever(httpClient), _retriever);
    }

    public void Dispose()
    {
        _retriever.Dispose();
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

    private string[] SavedPages() =>
        Directory.Exists(_outputFolder)
            ? Directory.GetFiles(Path.Combine(_outputFolder, "html"), "*.html", SearchOption.AllDirectories)
            : [];

    private string[] CsvFiles() => Directory.GetFiles(_outputFolder, "*.csv");

    [Fact]
    public async Task Archives_every_url_when_not_interrupted()
    {
        await _archiver.ArchiveHtmlAsync(_options, CancellationToken.None, new RunController()).WaitAsync(Timeout);

        Assert.Equal(5, _handler.Requests.Count);
        Assert.Equal(5, SavedPages().Length);
        Assert.Single(CsvFiles());
    }

    [Fact]
    public async Task Makes_no_requests_when_quit_before_starting()
    {
        var run = new RunController();
        run.Quit();

        await _archiver.ArchiveHtmlAsync(_options, CancellationToken.None, run).WaitAsync(Timeout);

        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Quit_stops_before_the_next_page_and_still_writes_results()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Quit());

        await _archiver.ArchiveHtmlAsync(_options, CancellationToken.None, run).WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
        Assert.Single(SavedPages());
        Assert.Single(CsvFiles());
    }

    [Fact]
    public async Task Pause_holds_pages_until_resumed()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Pause());

        var task = _archiver.ArchiveHtmlAsync(_options, CancellationToken.None, run);

        await Task.Delay(Settle);

        Assert.Single(_handler.Requests);
        Assert.False(task.IsCompleted);

        run.Resume();
        await task.WaitAsync(Timeout);

        Assert.Equal(5, _handler.Requests.Count);
        Assert.Equal(5, SavedPages().Length);
    }

    [Fact]
    public async Task Quit_while_paused_ends_the_run_and_writes_results()
    {
        var run = new RunController();
        OnFirstRequest(() => run.Pause());

        var task = _archiver.ArchiveHtmlAsync(_options, CancellationToken.None, run);

        await Task.Delay(Settle);
        run.Quit();
        await task.WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
        Assert.Single(CsvFiles());
    }

    [Fact]
    public async Task Cancelling_while_paused_ends_the_run_and_writes_results()
    {
        var run = new RunController();
        using var cts = new CancellationTokenSource();
        OnFirstRequest(() => run.Pause());

        var task = _archiver.ArchiveHtmlAsync(_options, cts.Token, run);

        await Task.Delay(Settle);
        await cts.CancelAsync();
        await task.WaitAsync(Timeout);

        Assert.Single(_handler.Requests);
        Assert.Single(CsvFiles());
    }

    [Fact]
    public async Task Quit_stops_the_spider_from_starting_another_pass()
    {
        _handler.AddHtml("https://example.com/page1", "<html><body><a href=\"/spidered\">more</a></body></html>");
        var urlsPath = Path.Combine(_tempFolder, "one.txt");
        await File.WriteAllLinesAsync(urlsPath, ["https://example.com/page1"]);
        var options = new PageArchiveOptions
        {
            SitemapUrl = urlsPath,
            OutputPath = _outputFolder,
            ThreadCount = 1,
            PrimaryDomain = "example.com",
            IsSpiderEnabled = true,
        };
        var run = new RunController();
        OnFirstRequest(() => run.Quit());

        await _archiver.ArchiveHtmlAsync(options, CancellationToken.None, run).WaitAsync(Timeout);

        Assert.DoesNotContain(_handler.Requests, x => x.Url.Contains("spidered"));
    }
}
