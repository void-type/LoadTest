using LoadTest.Helpers;
using System.Xml;
using System.Xml.Linq;

namespace LoadTest.Services;

public class UrlsRetriever
{
    private readonly HttpClient _httpClient;

    public UrlsRetriever(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get URLs from a local file or local/remote sitemap.
    /// </summary>
    public async Task<string[]> GetUrlsAsync(string path, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        var urls = File.Exists(path) && !path.EndsWith(".xml")
            ? await GetUrlsFromUrlListFileAsync(path, cancellationToken)
            : await GetUrlsFromSitemapUrlAsync(path, customHeaders, userAgent, cancellationToken);

        Console.WriteLine($"Found {urls.Length} URLs.");

        return urls;
    }

    /// <summary>
    /// Get URLs and save them to a local file.
    /// </summary>
    public async Task SaveUrlsAsync(string path, string outputPath, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        var urls = await GetUrlsAsync(path, customHeaders, userAgent, cancellationToken);

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

        var urls = await GetUrlsFromSitemapRecursiveAsync(sitemapUrl, customHeaders, userAgent, cancellationToken);

        return urls
            .Distinct()
            .ToArray();
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
    /// Can get XML from a file or URL.
    /// </summary>
    private async Task<XElement> GetSitemapXmlAsync(string sitemapUrl, List<string>? customHeaders, string? userAgent, CancellationToken cancellationToken)
    {
        if (File.Exists(sitemapUrl))
        {
            var fileContent = await File.ReadAllTextAsync(sitemapUrl, cancellationToken);
            return XElement.Parse(fileContent);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, sitemapUrl);
        HttpRequestHelper.ApplyHeaders(request, customHeaders, userAgent);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var xmlString = await response.Content.ReadAsStringAsync(cancellationToken);
        return XElement.Parse(xmlString);
    }
}
