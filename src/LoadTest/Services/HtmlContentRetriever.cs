using PuppeteerSharp;

namespace LoadTest.Services;

public class HtmlContentRetriever : IDisposable
{
    private HttpClient? _httpClient;
    private IBrowser? _browser;
    private readonly PageArchiverConfiguration _config;
    private bool _disposedValue;

    public HtmlContentRetriever(PageArchiverConfiguration config)
    {
        _config = config;
    }

    public async Task Init()
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            DefaultViewport = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            }
        });
    }

    public async Task<string> GetContentAsync(string url, LoadTesterThreadMetrics metrics, CancellationToken cancellationToken)
    {
        return _config.UseBrowser ?
            await GetBrowserContentAsync(url, metrics, cancellationToken) :
            await GetServerContentAsync(url, metrics, cancellationToken);
    }

    private async Task<string> GetBrowserContentAsync(string url, LoadTesterThreadMetrics metrics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var page = await Browser.NewPageAsync();

        page.PageError += (sender, eventArgs) =>
        {
            if (_config.LogBrowserConsoleError)
            {
                Console.WriteLine($"Browser console error on {url}: {eventArgs.Message}");
            }
        };

        var response = await page.GoToAsync(url);

        metrics.RequestCount++;

        if (!(((int)response.Status >= 200) && ((int)response.Status <= 299)))
        {
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.Status} ({response.Status}).");
        }

        if (_config.IsVerbose)
        {
            Console.WriteLine($"{response.Status} {url}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Wait for JS to render (you may need to adjust the wait time)
        // Alternatively, you could listen for JavaScript event if you can make the app emit one when it's done with initial rendering.
        await page.WaitForTimeoutAsync(100);

        return await page.GetContentAsync();
    }

    private async Task<string> GetServerContentAsync(string url, LoadTesterThreadMetrics metrics, CancellationToken cancellationToken)
    {
        var client = HttpClient;

        var response = await client.GetAsync(url, cancellationToken);

        metrics.RequestCount++;

        response.EnsureSuccessStatusCode();

        if (_config.IsVerbose)
        {
            Console.WriteLine($"{response.StatusCode} {url}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private IBrowser Browser => _browser ?? throw new InvalidOperationException("Call Init() before calling GetContent()");

    private HttpClient HttpClient => _httpClient ??= new();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _browser?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
