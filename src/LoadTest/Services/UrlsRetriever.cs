using System.Xml;
using System.Xml.Linq;

namespace LoadTest.Services;

public static class UrlsRetriever
{
    /// <summary>
    /// Get URLs from a local file or remote sitemap.
    /// </summary>
    public static async Task<string[]> GetUrls(string path)
    {
        return File.Exists(path)
            ? await GetUrlsFromUrlListFile(path)
            : await GetUrlsFromSitemapUrl(path);
    }

    /// <summary>
    /// Get URLs and save them to a local file.
    /// </summary>
    public static async Task<int> SaveUrls(string path, string outputPath)
    {
        var urls = await GetUrls(path);

        Console.WriteLine($"Writing URLs to {outputPath}.");
        await File.WriteAllLinesAsync(outputPath, urls);
        return 0;
    }

    private static async Task<string[]> GetUrlsFromUrlListFile(string filePath)
    {
        Console.WriteLine("Getting URLs from file.");
        return (await File.ReadAllLinesAsync(filePath))
            .Distinct()
            .ToArray();
    }

    private static async Task<string[]> GetUrlsFromSitemapUrl(string sitemapUrl)
    {
        Console.WriteLine("Getting URLs from sitemap.");
        using var httpClient = new HttpClient();

        var urls = await GetUrlsFromSitemapRecursive(httpClient, sitemapUrl);

        return urls
            .Distinct()
            .ToArray();
    }

    private static async Task<List<string>> GetUrlsFromSitemapRecursive(HttpClient httpClient, string sitemapUrl)
    {
        try
        {
            var xmlString = await httpClient.GetStringAsync(sitemapUrl);
            var xml = XElement.Parse(xmlString);
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

                urls.AddRange(locs);
            }

            var childSitemapUrls = xml
                .DescendantsAndSelf()
                .Where(x => x.Name.LocalName == "sitemap")
                .SelectMany(sitemap => sitemap.Descendants()
                    .Where(loc => loc.Name.LocalName == "loc")
                    .Select(x => x.Value));

            foreach (var childSitemapUrl in childSitemapUrls)
            {
                urls.AddRange(await GetUrlsFromSitemapRecursive(httpClient, childSitemapUrl));
            }

            return urls;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving sitemap at {sitemapUrl} (Status Code: {ex.StatusCode}).");

            return new();
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"Error parsing XML of sitemap at {sitemapUrl} (Exception: {ex.Message}).");

            return new();
        }
    }
}
