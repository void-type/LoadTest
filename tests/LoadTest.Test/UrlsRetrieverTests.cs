namespace LoadTest.Test;

using LoadTest.Services;
using System.IO.Compression;
using System.Net;
using System.Text;

public sealed class UrlsRetrieverTests : IDisposable
{
    private const string UrlSetXmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "LoadTestTests", Guid.NewGuid().ToString("N"));

    public UrlsRetrieverTests()
    {
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, recursive: true);
    }

    private static string UrlSet(params string[] locs) =>
        $"<urlset xmlns=\"{UrlSetXmlns}\">{string.Concat(locs.Select(x => $"<url><loc>{x}</loc></url>"))}</urlset>";

    private static string SitemapIndex(params string[] locs) =>
        $"<sitemapindex xmlns=\"{UrlSetXmlns}\">{string.Concat(locs.Select(x => $"<sitemap><loc>{x}</loc></sitemap>"))}</sitemapindex>";

    private static Task<string[]> GetUrlsAsync(FakeHttpMessageHandler handler, string path, List<string> headers = null, string userAgent = null)
    {
        return new UrlsRetriever(handler.CreateClient()).GetUrlsAsync(path, headers, userAgent, CancellationToken.None);
    }

    [Fact]
    public async Task Reads_urls_from_a_plain_sitemap()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/sitemap.xml", UrlSet("https://example.com/a", "https://example.com/b"));

        var urls = await GetUrlsAsync(handler, "https://example.com/sitemap.xml");

        Assert.Equal(["https://example.com/a", "https://example.com/b"], urls);
    }

    [Fact]
    public async Task Includes_alternate_language_links_and_ignores_other_links()
    {
        var sitemap = $"""
            <urlset xmlns="{UrlSetXmlns}" xmlns:xhtml="http://www.w3.org/1999/xhtml">
              <url>
                <loc>https://example.com/en</loc>
                <xhtml:link rel="alternate" hreflang="fr" href="https://example.com/fr"/>
                <xhtml:link rel="canonical" href="https://example.com/canonical"/>
                <xhtml:link rel="alternate" hreflang="de" href=""/>
              </url>
            </urlset>
            """;
        var handler = new FakeHttpMessageHandler().Add("https://example.com/sitemap.xml", sitemap);

        var urls = await GetUrlsAsync(handler, "https://example.com/sitemap.xml");

        Assert.Equal(["https://example.com/en", "https://example.com/fr"], urls);
    }

    [Fact]
    public async Task Follows_nested_sitemap_indexes_and_dedupes()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/sitemap.xml", SitemapIndex("https://example.com/posts.xml", "https://example.com/more.xml"))
            .Add("https://example.com/posts.xml", UrlSet("https://example.com/a", "https://example.com/b"))
            .Add("https://example.com/more.xml", SitemapIndex("https://example.com/deep.xml"))
            .Add("https://example.com/deep.xml", UrlSet("https://example.com/b", "https://example.com/c"));

        var urls = await GetUrlsAsync(handler, "https://example.com/sitemap.xml");

        Assert.Equal(["https://example.com/a", "https://example.com/b", "https://example.com/c"], urls);
    }

    [Fact]
    public async Task A_failing_child_sitemap_does_not_stop_the_others()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/sitemap.xml", SitemapIndex("https://example.com/broken.xml", "https://example.com/bad-xml.xml", "https://example.com/missing.xml", "https://example.com/good.xml"))
            .Add("https://example.com/broken.xml", HttpStatusCode.InternalServerError)
            .Add("https://example.com/bad-xml.xml", "<urlset><url>")
            .Add("https://example.com/good.xml", UrlSet("https://example.com/a"));

        var urls = await GetUrlsAsync(handler, "https://example.com/sitemap.xml");

        Assert.Equal(["https://example.com/a"], urls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Returns_no_urls_when_sitemap_request_fails(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler().Add("https://example.com/sitemap.xml", statusCode);

        Assert.Empty(await GetUrlsAsync(handler, "https://example.com/sitemap.xml"));
    }

    [Fact]
    public async Task Returns_no_urls_when_sitemap_is_not_xml()
    {
        var handler = new FakeHttpMessageHandler().Add("https://example.com/sitemap.xml", "<html><body>Not a sitemap");

        Assert.Empty(await GetUrlsAsync(handler, "https://example.com/sitemap.xml"));
    }

    [Fact]
    public async Task Reads_gzipped_sitemaps()
    {
        using var compressed = new MemoryStream();

        await using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(UrlSet("https://example.com/a")));
        }

        var handler = new FakeHttpMessageHandler().Add("https://example.com/sitemap.xml.gz", compressed.ToArray());

        Assert.Equal(["https://example.com/a"], await GetUrlsAsync(handler, "https://example.com/sitemap.xml.gz"));
    }

    [Fact]
    public async Task Root_domain_uses_all_sitemaps_listed_in_robots_txt()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/robots.txt", "User-agent: *\nSitemap: https://example.com/one.xml\nSitemap: https://example.com/two.xml\n")
            .Add("https://example.com/one.xml", UrlSet("https://example.com/a"))
            .Add("https://example.com/two.xml", UrlSet("https://example.com/b"));

        var urls = await GetUrlsAsync(handler, "https://example.com");

        Assert.Equal(["https://example.com/a", "https://example.com/b"], urls);
        Assert.Equal("https://example.com/robots.txt", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Root_domain_without_sitemaps_in_robots_txt_falls_back_to_sitemap_xml()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/robots.txt", "User-agent: *\nDisallow: /admin\n")
            .Add("https://example.com/sitemap.xml", UrlSet("https://example.com/a"));

        Assert.Equal(["https://example.com/a"], await GetUrlsAsync(handler, "https://example.com"));
        Assert.Equal(["https://example.com/robots.txt", "https://example.com/sitemap.xml"], handler.Requests.Select(x => x.Url));
    }

    [Fact]
    public async Task Root_domain_with_missing_robots_txt_falls_back_to_sitemap_xml()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com:8080/sitemap.xml", UrlSet("https://example.com:8080/a"));

        Assert.Equal(["https://example.com:8080/a"], await GetUrlsAsync(handler, "https://example.com:8080/"));
    }

    [Fact]
    public async Task Root_domain_returns_no_urls_when_robots_txt_and_sitemap_xml_are_missing()
    {
        var handler = new FakeHttpMessageHandler();

        Assert.Empty(await GetUrlsAsync(handler, "https://example.com/"));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Root_domain_does_not_fall_back_when_robots_txt_lists_sitemaps()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/robots.txt", "Sitemap: https://example.com/custom.xml")
            .Add("https://example.com/custom.xml", UrlSet("https://example.com/a"));

        await GetUrlsAsync(handler, "https://example.com");

        Assert.DoesNotContain(handler.Requests, x => x.Url == "https://example.com/sitemap.xml");
    }

    [Fact]
    public async Task Non_root_urls_do_not_request_robots_txt()
    {
        var handler = new FakeHttpMessageHandler().Add("https://example.com/en", UrlSet("https://example.com/a"));

        await GetUrlsAsync(handler, "https://example.com/en");

        Assert.Equal(["https://example.com/en"], handler.Requests.Select(x => x.Url));
    }

    [Fact]
    public async Task Sends_user_agent_and_custom_headers_on_every_request()
    {
        var handler = new FakeHttpMessageHandler()
            .Add("https://example.com/robots.txt", "Sitemap: https://example.com/sitemap.xml")
            .Add("https://example.com/sitemap.xml", SitemapIndex("https://example.com/child.xml"))
            .Add("https://example.com/child.xml", UrlSet("https://example.com/a"));

        await GetUrlsAsync(handler, "https://example.com", ["X-Test: 1"], "MyAgent/1.0");

        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("1", request.Headers["X-Test"]);
            Assert.Equal("MyAgent/1.0", request.Headers["User-Agent"]);
        });
    }

    [Fact]
    public async Task Reads_url_list_files_and_dedupes_lines()
    {
        var path = Path.Combine(_tempFolder, "urls.txt");
        await File.WriteAllLinesAsync(path, ["https://example.com/a", "https://example.com/b", "https://example.com/a"]);
        var handler = new FakeHttpMessageHandler();

        var urls = await GetUrlsAsync(handler, path);

        Assert.Equal(["https://example.com/a", "https://example.com/b"], urls);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Reads_local_sitemap_files()
    {
        var path = Path.Combine(_tempFolder, "sitemap.xml");
        await File.WriteAllTextAsync(path, UrlSet("https://example.com/a", "https://example.com/b"));
        var handler = new FakeHttpMessageHandler();

        var urls = await GetUrlsAsync(handler, path);

        Assert.Equal(["https://example.com/a", "https://example.com/b"], urls);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Reads_local_gzipped_sitemap_files()
    {
        var path = Path.Combine(_tempFolder, "sitemap.xml.gz");

        await using (var file = File.Create(path))
        await using (var gzip = new GZipStream(file, CompressionMode.Compress))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(UrlSet("https://example.com/a")));
        }

        Assert.Equal(["https://example.com/a"], await GetUrlsAsync(new FakeHttpMessageHandler(), path));
    }

    [Fact]
    public async Task Local_sitemap_index_follows_remote_children()
    {
        var path = Path.Combine(_tempFolder, "index.xml");
        await File.WriteAllTextAsync(path, SitemapIndex("https://example.com/child.xml"));
        var handler = new FakeHttpMessageHandler().Add("https://example.com/child.xml", UrlSet("https://example.com/a"));

        Assert.Equal(["https://example.com/a"], await GetUrlsAsync(handler, path));
    }

    [Fact]
    public async Task SaveUrlsAsync_normalizes_dedupes_and_writes_urls()
    {
        var outputPath = Path.Combine(_tempFolder, "out.txt");
        var handler = new FakeHttpMessageHandler().Add(
            "https://example.com/sitemap.xml",
            UrlSet(
                "https://Example.com/About/",
                "http://www.example.com/about",
                "https://example.com/about?utm=1#top",
                "https://example.com/other",
                "  "));

        await new UrlsRetriever(handler.CreateClient()).SaveUrlsAsync(
            "https://example.com/sitemap.xml", outputPath, null, null, "example.com", ["www.example.com"], CancellationToken.None);

        Assert.Equal(
            ["https://example.com/about", "https://example.com/other"],
            await File.ReadAllLinesAsync(outputPath));
    }

    [Fact]
    public async Task SaveUrlsAsync_writes_an_empty_file_when_there_are_no_urls()
    {
        var outputPath = Path.Combine(_tempFolder, "out.txt");

        await new UrlsRetriever(new FakeHttpMessageHandler().CreateClient()).SaveUrlsAsync(
            "https://example.com/sitemap.xml", outputPath, null, null, "example.com", [], CancellationToken.None);

        Assert.Empty(await File.ReadAllLinesAsync(outputPath));
    }
}
