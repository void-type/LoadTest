using LoadTest.Helpers;
using LoadTest.Models;
using PuppeteerSharp;

namespace LoadTest.Services;

public class HtmlContentRetriever : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(1);
    private IBrowser? _browser;
    private bool _disposedValue;

    public HtmlContentRetriever(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HtmlContentRetrieverResult> GetContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        return config.UseBrowser ?
            await GetBrowserContentAsync(config, uri, cancellationToken) :
            await GetServerContentAsync(config, uri, cancellationToken);
    }

    private async Task<HtmlContentRetrieverResult> GetBrowserContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        var result = new HtmlContentRetrieverResult();

        try
        {
            var browser = await EnsureBrowserAsync(config, cancellationToken);

            await using var page = await browser.NewPageAsync();

            page.PageError += (sender, eventArgs) =>
            {
                if (config.LogBrowserConsoleErrors)
                {
                    Console.WriteLine($"Browser console error on {uri.OriginalString}: {eventArgs.Message}");
                }
            };

            var extraHeaders = HttpRequestHelper.GetExtraHeaders(config.CustomHeaders);

            if (extraHeaders.Count > 0)
            {
                await page.SetExtraHttpHeadersAsync(extraHeaders);
            }

            var response = await page.GoToAsync(uri.OriginalString);

            var isSuccess = response.IsSuccessStatusCode();
            var isHtml = response.Headers["content-type"]?.Contains("text/html", StringComparison.OrdinalIgnoreCase) ?? false;

            if (config.IsVerbose)
            {
                Console.WriteLine($"{response.Status} {uri.OriginalString} - IsHtml: {isHtml}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Wait for JS to render (you may need to adjust the wait time)
            // Alternatively, you could listen for JavaScript event if you can make the app emit one when it's done with initial rendering.
            await Task.Delay(100, cancellationToken);

            result.FinalUrl = response.Url.GetNormalizedUri(config.PrimaryDomain, config.PrimaryDomainEquivalents, uri.ToString());
            result.StatusCode = (int)response.Status;
            result.IsRetrieveError = !(isHtml && isSuccess);

            if (result.IsRetrieveError)
            {
                Console.WriteLine($"Failed to retrieve HTML content for {uri.OriginalString}. StatusCode: {response.Status}, IsHtml: {isHtml}");
            }
            else
            {
                result.HtmlContent = await page.GetContentAsync();
            }
        }
        catch (OperationCanceledException)
        {
            result.IsRetrieveError = true;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HTML content for {uri.OriginalString}: {ex.Message}");
            result.IsRetrieveError = true;
        }

        return result;
    }

    private async Task<HtmlContentRetrieverResult> GetServerContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        var result = new HtmlContentRetrieverResult();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            HttpRequestHelper.ApplyHeaders(request, config.CustomHeaders, config.UserAgent);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            var isSuccess = response.IsSuccessStatusCode();
            var isHtml = response.Content.Headers.ContentType?.MediaType?.Equals("text/html", StringComparison.OrdinalIgnoreCase) ?? false;

            if (config.IsVerbose)
            {
                Console.WriteLine($"{response.StatusCode} {uri.OriginalString} - IsHtml: {isHtml}");
            }

            result.FinalUrl = response.RequestMessage?.RequestUri?.OriginalString
                .GetNormalizedUri(config.PrimaryDomain, config.PrimaryDomainEquivalents, uri.ToString());
            result.StatusCode = (int)response.StatusCode;
            result.IsRetrieveError = !(isHtml && isSuccess);

            if (result.IsRetrieveError)
            {
                Console.WriteLine($"Failed to retrieve HTML content for {uri.OriginalString}. StatusCode: {response.StatusCode}, IsHtml: {isHtml}");
            }
            else
            {
                result.HtmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            result.IsRetrieveError = true;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HTML content for {uri.OriginalString}: {ex.Message}");
            result.IsRetrieveError = true;
        }

        return result;
    }

    private async Task<IBrowser> EnsureBrowserAsync(PageArchiveOptions config, CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _browser ??= await CreateBrowserAsync(config);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<IBrowser> CreateBrowserAsync(PageArchiveOptions config)
    {
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            DefaultViewport = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            }
        };

        if (!string.IsNullOrWhiteSpace(config.UserAgent))
        {
            launchOptions.Args = new[] { $"--user-agent={config.UserAgent}" };
        }

        return await Puppeteer.LaunchAsync(launchOptions);
    }

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
