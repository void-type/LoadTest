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
    public async Task<string[]> GetUrlsAsync(string path, CancellationToken cancellationToken)
    {
        var urls = File.Exists(path) && !path.EndsWith(".xml")
            ? await GetUrlsFromUrlListFileAsync(path, cancellationToken)
            : await GetUrlsFromSitemapUrlAsync(path, cancellationToken);

        Console.WriteLine($"Found {urls.Length} URLs.");

        return urls;
    }

    /// <summary>
    /// Get URLs and save them to a local file.
    /// </summary>
    public async Task SaveUrlsAsync(string path, string outputPath, CancellationToken cancellationToken)
    {
        var urls = await GetUrlsAsync(path, cancellationToken);

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

    private async Task<string[]> GetUrlsFromSitemapUrlAsync(string sitemapUrl, CancellationToken cancellationToken)
    {
        Console.WriteLine("Getting URLs from sitemap.");

        var urls = await GetUrlsFromSitemapRecursiveAsync(sitemapUrl, cancellationToken);

        return urls
            .Distinct()
            .ToArray();
    }

    private async Task<List<string>> GetUrlsFromSitemapRecursiveAsync(string sitemapUrl, CancellationToken cancellationToken)
    {
        try
        {
            var xml = await GetSitemapXmlAsync(sitemapUrl, cancellationToken);

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
                urls.AddRange(await GetUrlsFromSitemapRecursiveAsync(childSitemapUrl, cancellationToken));
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
    private async Task<XElement> GetSitemapXmlAsync(string sitemapUrl, CancellationToken cancellationToken)
    {
        var xmlString = File.Exists(sitemapUrl) ?
            await File.ReadAllTextAsync(sitemapUrl, cancellationToken) :
            await _httpClient.GetStringAsync(sitemapUrl, cancellationToken);

        return XElement.Parse(xmlString);
    }
}
