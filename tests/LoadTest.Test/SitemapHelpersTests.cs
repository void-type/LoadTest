namespace LoadTest.Test;

using LoadTest.Helpers;
using System.IO.Compression;
using System.Text;

public class SitemapHelpersTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/robots.txt")]
    [InlineData("https://example.com/", "https://example.com/robots.txt")]
    [InlineData("http://example.com:8080/", "http://example.com:8080/robots.txt")]
    [InlineData("https://example.com/sitemap.xml", null)]
    [InlineData("https://example.com/en", null)]
    [InlineData("https://example.com/?q=1", null)]
    [InlineData("./urls.txt", null)]
    [InlineData("example.com", null)]
    public void GetRobotsUri_only_for_root_domains(string path, string expected)
    {
        Assert.Equal(expected, SitemapHelpers.GetRobotsUri(path)?.ToString());
    }

    [Fact]
    public void GetSitemapUrlsFromRobots_finds_all_sitemaps()
    {
        var robots = "User-agent: *\r\nDisallow: /admin\r\nsitemap: https://example.com/sitemap.xml\nSITEMAP:https://example.com/sitemap.xml.gz  \nSitemap: https://example.com/sitemap.xml\nSitemap: not-a-url\n";

        var urls = SitemapHelpers.GetSitemapUrlsFromRobots(robots);

        Assert.Equal(["https://example.com/sitemap.xml", "https://example.com/sitemap.xml.gz"], urls);
    }

    [Fact]
    public void GetSitemapUrlsFromRobots_returns_empty_without_sitemaps()
    {
        Assert.Empty(SitemapHelpers.GetSitemapUrlsFromRobots("User-agent: *\nDisallow: /"));
    }

    [Fact]
    public async Task DecodeSitemapAsync_reads_plain_content()
    {
        var content = Encoding.UTF8.GetBytes("<urlset />");

        Assert.Equal("<urlset />", await SitemapHelpers.DecodeSitemapAsync(content, CancellationToken.None));
    }

    [Fact]
    public async Task DecodeSitemapAsync_decompresses_gzip_content()
    {
        using var output = new MemoryStream();

        await using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes("<urlset />"), CancellationToken.None);
        }

        Assert.Equal("<urlset />", await SitemapHelpers.DecodeSitemapAsync(output.ToArray(), CancellationToken.None));
    }
}
