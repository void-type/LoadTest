namespace LoadTest.Test;

using LoadTest.Helpers;

public class HtmlScannerTests
{
    private static readonly Uri PageUri = new("https://example.com/blog/post");

    [Theory]
    // Root-relative
    [InlineData("<img src=\"/logo.png\">", "https://example.com/logo.png")]
    // Relative
    [InlineData("<img src=\"logo.png\">", "https://example.com/blog/logo.png")]
    // Absolute, same domain
    [InlineData("<img src=\"https://example.com/logo.png\">", "https://example.com/logo.png")]
    // Cache-busting query strings are preserved
    [InlineData("<img src=\"/logo.png?v=123\">", "https://example.com/logo.png?v=123")]
    // Stylesheet link
    [InlineData("<link rel=\"stylesheet\" href=\"/site.css\">", "https://example.com/site.css")]
    // Stylesheet rel is matched case-insensitively
    [InlineData("<link rel=\"Stylesheet\" href=\"/site.css\">", "https://example.com/site.css")]
    // Stylesheet in a multi-token rel value
    [InlineData("<link rel=\"stylesheet preload\" href=\"/site.css\">", "https://example.com/site.css")]
    // Preloaded resources are fetched by a browser, so they count as load
    [InlineData("<link rel=\"preload\" as=\"style\" href=\"/site.css\">", "https://example.com/site.css")]
    [InlineData("<link rel=\"preload\" as=\"script\" href=\"/app.js\">", "https://example.com/app.js")]
    [InlineData("<link rel=\"preload\" as=\"font\" href=\"/font.woff2\">", "https://example.com/font.woff2")]
    // Script tag
    [InlineData("<script src=\"/app.js\"></script>", "https://example.com/app.js")]
    public async Task FindResourceLinksAsync_resolves_urls(string html, string expectedUrl)
    {
        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, null, CancellationToken.None);

        Assert.Equal([new Uri(expectedUrl)], links);
    }

    [Theory]
    // Non-http schemes are ignored
    [InlineData("<img src=\"data:image/png;base64,abc\">")]
    // Anchor tags are not resources, that's the spider's job
    [InlineData("<a href=\"/page2\">Link</a>")]
    // Non-stylesheet link tags are ignored
    [InlineData("<link rel=\"canonical\" href=\"/page\">")]
    // Preload without an 'as' has no known type, so it's ignored
    [InlineData("<link rel=\"preload\" href=\"/thing\">")]
    // Preload of a non-static-asset type is ignored
    [InlineData("<link rel=\"preload\" as=\"fetch\" href=\"/data.json\">")]
    // Missing attribute
    [InlineData("<img>")]
    public async Task FindResourceLinksAsync_ignores_non_resource_or_non_http(string html)
    {
        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, null, CancellationToken.None);

        Assert.Empty(links);
    }

    [Fact]
    public async Task FindResourceLinksAsync_dedupes_links()
    {
        var html = """
            <img src="/logo.png">
            <img src="/logo.png">
            """;

        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, null, CancellationToken.None);

        Assert.Equal([new Uri("https://example.com/logo.png")], links);
    }

    [Theory]
    // Protocol-relative, cross domain
    [InlineData("<img src=\"//cdn.example.com/logo.png\">")]
    // Absolute, cross domain
    [InlineData("<script src=\"https://cdn.example.com/app.js\"></script>")]
    public async Task FindResourceLinksAsync_excludes_cross_domain_by_default(string html)
    {
        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, null, CancellationToken.None);

        Assert.Empty(links);
    }

    [Fact]
    public async Task FindResourceLinksAsync_includes_extra_allowed_domains()
    {
        var html = "<img src=\"//cdn.example.com/logo.png\">";

        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, ["cdn.example.com"], CancellationToken.None);

        Assert.Equal([new Uri("https://cdn.example.com/logo.png")], links);
    }

    [Fact]
    public async Task FindResourceLinksAsync_allowed_domain_match_is_case_insensitive()
    {
        var html = "<img src=\"//CDN.example.com/logo.png\">";

        var links = await HtmlScanner.FindResourceLinksAsync(PageUri, html, ["cdn.example.com"], CancellationToken.None);

        Assert.Equal([new Uri("https://cdn.example.com/logo.png")], links);
    }
}
