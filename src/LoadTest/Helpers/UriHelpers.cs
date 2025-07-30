using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class UriHelpers
{
    /// <summary>
    /// Will normalize local URLs to be fully-qualified, and will remove query strings and fragments.
    /// </summary>
    public static Uri? GetNormalizedUri(this string url, string? primaryDomain, string[]? primaryDomainEquivalents, string? foundAt, bool quiet = false)
    {
        try
        {
            // Basic cleanup
            url = url.Trim().ToLowerInvariant().TrimStart('~');
            primaryDomain = primaryDomain?.ToLowerInvariant();

            // Protocol normalization
            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }
            else if (url.StartsWith("http://"))
            {
                url = "https:" + url[5..];
            }

            // Relative URL resolution
            if (url.StartsWith('/'))
            {
                var hostToUse = !string.IsNullOrWhiteSpace(foundAt) ? new Uri(foundAt).Host : primaryDomain;

                if (!string.IsNullOrWhiteSpace(hostToUse))
                {
                    var baseUri = new Uri($"https://{hostToUse}");
                    var resolvedUri = new Uri(baseUri, url);
                    url = resolvedUri.ToString();
                }
            }

            // Ensure https for external URLs
            if (!url.StartsWith("https://") && !url.Contains("://"))
            {
                url = "https://" + url;
            }

            var uriBuilder = new UriBuilder(url);

            // Port normalization - remove default ports
            if (uriBuilder.Port == 80 || uriBuilder.Port == 443)
            {
                uriBuilder.Port = -1;
            }

            // Query and fragment removal
            uriBuilder.Query = string.Empty;
            uriBuilder.Fragment = string.Empty;

            // Trailing slash normalization
            if (uriBuilder.Path.Length > 1 && uriBuilder.Path.EndsWith('/'))
            {
                uriBuilder.Path = uriBuilder.Path.TrimEnd('/');
            }

            // Domain equivalents normalization
            if (primaryDomainEquivalents?.Any(x => x.EqualsIgnoreCase(uriBuilder.Host)) == true)
            {
                uriBuilder.Host = primaryDomain;
            }

            return uriBuilder.Uri;
        }
        catch (Exception ex)
        {
            if (!quiet)
            {
                Console.WriteLine($"Error normalizing {url}. {ex.Message}");
            }

            return null;
        }
    }
}
