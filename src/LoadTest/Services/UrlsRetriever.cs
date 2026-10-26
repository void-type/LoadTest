using LoadTest.Helpers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LoadTest.Services;

public class UrlsRetriever(HttpClient httpClient)
{
    /// <summary>
    /// Get URLs from a local file, local/remote sitemap (plain or gzipped), or a root domain whose robots.txt lists sitemaps.
    /// </summary>
    public async Task<string[]> GetUrlsAsync(string path, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        var urls = File.Exists(path) && !path.EndsWith(".xml") && !path.EndsWith(".gz")
            ? await GetUrlsFromUrlListFileAsync(path, cancellationToken)
            : await GetUrlsFromSitemapUrlAsync(path, customHeaders, userAgent, cancellationToken);

        Console.WriteLine($"Found {urls.Length} URLs.");

        return urls;
    }

    /// <summary>
    /// Get URLs and save them to a local file.
    /// </summary>
    public async Task SaveUrlsAsync(string path, string outputPath, List<string>? customHeaders, string? userAgent,
        string? primaryDomain, string[]? primaryDomainEquivalents, CancellationToken cancellationToken)
    {
        var urls = (await GetUrlsAsync(path, customHeaders, userAgent, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.GetNormalizedUri(primaryDomain, primaryDomainEquivalents, null))
            .Where(x => x is not null)
            .Select(x => x!.ToString())
            .Distinct()
            .ToArray();

        Console.WriteLine($"Writing URLs to {outputPath}.");
        await File.WriteAllLinesAsync(outputPath, urls, cancellationToken);
    }

    private static async Task<string[]> GetUrlsFromUrlListFileAsync(string filePath, CancellationToken cancellationToken)
    {
        Console.WriteLine("Getting URLs from file.");
        return (await File.ReadAllLinesAsync(filePath, cancellationToken))
            .Distinct()
            .ToArray();
    }

    private async Task<string[]> GetUrlsFromSitemapUrlAsync(string sitemapUrl, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        Console.WriteLine("Getting URLs from sitemap.");

        var sitemapUrls = await GetSitemapUrlsFromRobotsAsync(sitemapUrl, customHeaders, userAgent, cancellationToken) ?? [sitemapUrl];

        var urls = new List<string>();

        foreach (var url in sitemapUrls)
        {
            urls.AddRange(await GetUrlsFromSitemapRecursiveAsync(url, customHeaders, userAgent, cancellationToken));
        }

        return urls
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// If the path is a root domain, finds sitemap URLs in its robots.txt. Returns null if the path isn't a root domain.
    /// </summary>
    private async Task<string[]?> GetSitemapUrlsFromRobotsAsync(string path, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        var robotsUri = SitemapHelpers.GetRobotsUri(path);

        if (robotsUri is null)
        {
            return null;
        }

        Console.WriteLine($"Looking for sitemaps in {robotsUri}.");

        try
        {
            var robotsContent = Encoding.UTF8.GetString(await GetBytesAsync(robotsUri.ToString(), customHeaders, userAgent, cancellationToken));
            var sitemapUrls = SitemapHelpers.GetSitemapUrlsFromRobots(robotsContent);

            if (sitemapUrls.Length == 0)
            {
                Console.WriteLine($"No sitemaps found in {robotsUri}.");
            }

            return sitemapUrls;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving robots.txt at {robotsUri} (Status Code: {ex.StatusCode}).");

            return [];
        }
    }

    private async Task<List<string>> GetUrlsFromSitemapRecursiveAsync(string sitemapUrl, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        try
        {
            var xml = await GetSitemapXmlAsync(sitemapUrl, customHeaders, userAgent, cancellationToken);

            var urls = new List<string>();

            var urlSet = xml.DescendantsAndSelf()
                .FirstOrDefault(x => x.Name.LocalName == "urlset");

            if (urlSet is not null)
            {
                var locs = xml.Descendants()
                    .Where(x => x.Name.LocalName == "loc")
                    .Select(x => x.Value);

                urls.AddRange(locs);

                var alts = xml.Descendants()
                    .Where(x => x.Name.LocalName == "link" && x.Attribute("rel")?.Value == "alternate" && !string.IsNullOrWhiteSpace(x.Attribute("href")?.Value))
                    .Select(x => x.Attribute("href")!.Value);

                urls.AddRange(alts);
            }

            var childSitemapUrls = xml
                .DescendantsAndSelf()
                .Where(x => x.Name.LocalName == "sitemap")
                .SelectMany(sitemap => sitemap.Descendants()
                    .Where(loc => loc.Name.LocalName == "loc")
                    .Select(x => x.Value));

            foreach (var childSitemapUrl in childSitemapUrls)
            {
                Console.WriteLine($"Following child sitemap at {childSitemapUrl}.");
                urls.AddRange(await GetUrlsFromSitemapRecursiveAsync(childSitemapUrl, customHeaders, userAgent, cancellationToken));
            }

            return urls;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving sitemap at {sitemapUrl} (Status Code: {ex.StatusCode}).");

            return [];
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"Error parsing XML of sitemap at {sitemapUrl} (Exception: {ex.Message}).");

            return [];
        }
    }

    /// <summary>
    /// Can get XML from a file or URL. Gzipped sitemaps are decompressed.
    /// </summary>
    private async Task<XElement> GetSitemapXmlAsync(string sitemapUrl, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        var content = File.Exists(sitemapUrl)
            ? await File.ReadAllBytesAsync(sitemapUrl, cancellationToken)
            : await GetBytesAsync(sitemapUrl, customHeaders, userAgent, cancellationToken);

        return XElement.Parse(await SitemapHelpers.DecodeSitemapAsync(content, cancellationToken));
    }

    private async Task<byte[]> GetBytesAsync(string url, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        HttpRequestHelper.ApplyHeaders(request, customHeaders, userAgent);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
