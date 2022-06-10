using System.Xml;
using System.Xml.Linq;

namespace LoadTest;

public static class SitemapHelpers
{
    public static async Task<string[]> GetUrlsFromUrlListFile(string filePath)
    {
        return (await File.ReadAllLinesAsync(filePath))
            .Distinct()
            .ToArray();
    }

    public static async Task<string[]> GetUrlsFromSitemapUrl(string sitemapUrl, bool isVerbose)
    {
        using var httpClient = new HttpClient();

        var urls = await GetUrlsFromSitemapRecursive(httpClient, sitemapUrl, isVerbose);

        return urls
            .Distinct()
            .ToArray();
    }

    private static async Task<List<string>> GetUrlsFromSitemapRecursive(HttpClient httpClient, string sitemapUrl, bool isVerbose)
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
                urls.AddRange(await GetUrlsFromSitemapRecursive(httpClient, childSitemapUrl, isVerbose));
            }

            return urls;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving sitemap at {sitemapUrl} (Status Code: {ex.StatusCode}).");

            if (isVerbose)
            {
                Console.WriteLine(ex);
            }

            return new();
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"Error parsing XML of sitemap at {sitemapUrl}.");

            if (isVerbose)
            {
                Console.WriteLine(ex);
            }

            return new();
        }
    }
}
