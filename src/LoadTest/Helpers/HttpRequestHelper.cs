namespace LoadTest.Helpers;

public static class HttpRequestHelper
{
    /// <summary>
    /// Apply custom headers and User-Agent to an HttpRequestMessage.
    /// </summary>
    public static void ApplyHeaders(HttpRequestMessage request, List<string>? customHeaders, string? userAgent)
    {
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            request.Headers.Add("User-Agent", userAgent);
        }

        if (customHeaders is null)
        {
            return;
        }

        foreach (var header in customHeaders)
        {
            var separatorIndex = header.IndexOf(':');

            if (separatorIndex < 1)
            {
                continue;
            }

            var key = header[..separatorIndex].Trim();
            var value = header[(separatorIndex + 1)..].Trim();

            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    /// <summary>
    /// Get custom headers as a dictionary for Puppeteer's SetExtraHttpHeadersAsync.
    /// </summary>
    public static Dictionary<string, string> GetExtraHeaders(List<string>? customHeaders)
    {
        var headers = new Dictionary<string, string>();

        if (customHeaders is null)
        {
            return headers;
        }

        foreach (var header in customHeaders)
        {
            var separatorIndex = header.IndexOf(':');

            if (separatorIndex < 1)
            {
                continue;
            }

            var key = header[..separatorIndex].Trim();
            var value = header[(separatorIndex + 1)..].Trim();

            headers[key] = value;
        }

        return headers;
    }
}
