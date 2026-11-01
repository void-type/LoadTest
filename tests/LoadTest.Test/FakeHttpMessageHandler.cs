namespace LoadTest.Test;

using System.Net;

/// <summary>
/// Serves canned responses by URL (404 for anything unregistered) and records the requests made.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RecordedRequest> _requests = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// A snapshot of the requests received so far.
    /// </summary>
    public List<RecordedRequest> Requests
    {
        get
        {
            lock (_lock)
            {
                return [.. _requests];
            }
        }
    }

    /// <summary>
    /// Called for each request as it arrives, before the response is returned.
    /// </summary>
    public Action<RecordedRequest> OnRequest { get; set; }

    public FakeHttpMessageHandler Add(string url, string content) =>
        Add(url, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });

    public FakeHttpMessageHandler AddHtml(string url, string html) =>
        Add(url, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html") });

    public FakeHttpMessageHandler Add(string url, byte[] content) =>
        Add(url, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });

    public FakeHttpMessageHandler Add(string url, HttpStatusCode statusCode) =>
        Add(url, () => new HttpResponseMessage(statusCode));

    private FakeHttpMessageHandler Add(string url, Func<HttpResponseMessage> response)
    {
        _responses[url] = response;
        return this;
    }

    public HttpClient CreateClient() => new(this);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();

        var recorded = new RecordedRequest(
            url,
            request.Headers.ToDictionary(x => x.Key, x => string.Join(",", x.Value), StringComparer.OrdinalIgnoreCase));

        lock (_lock)
        {
            _requests.Add(recorded);
        }

        OnRequest?.Invoke(recorded);

        return Task.FromResult(_responses.TryGetValue(url, out var response)
            ? response()
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public sealed record RecordedRequest(string Url, Dictionary<string, string> Headers);
