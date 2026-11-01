namespace LoadTest.Test;

using System.Net;

/// <summary>
/// Serves canned responses by URL (404 for anything unregistered) and records the requests made.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new(StringComparer.OrdinalIgnoreCase);

    public List<RecordedRequest> Requests { get; } = [];

    public FakeHttpMessageHandler Add(string url, string content) =>
        Add(url, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });

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

        Requests.Add(new RecordedRequest(
            url,
            request.Headers.ToDictionary(x => x.Key, x => string.Join(",", x.Value), StringComparer.OrdinalIgnoreCase)));

        return Task.FromResult(_responses.TryGetValue(url, out var response)
            ? response()
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public sealed record RecordedRequest(string Url, Dictionary<string, string> Headers);
