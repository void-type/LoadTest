namespace LoadTest.Test;

using LoadTest.Helpers;
using LoadTest.Models;

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

    private static PageArchiveOptions Options(
        string include = null,
        string exclude = null,
        List<string> terms = null,
        bool spider = false) => new()
        {
            PrimaryDomain = "example.com",
            PrimaryDomainEquivalents = ["www.example.com"],
            ContentIncludeSelector = include,
            ContentExcludeSelector = exclude,
            ContentSearchTerms = terms,
            IsSpiderEnabled = spider,
        };

    [Fact]
    public async Task ScanAsync_finds_search_terms_in_html_and_text()
    {
        var html = "<body><p>Hello <b>World</b></p><div class=\"promo\">secret</div></body>";

        var result = await HtmlScanner.ScanAsync(Options(terms: ["world", "<b>", "missing"]), PageUri.ToString(), html, CancellationToken.None);

        Assert.False(result.IsScanError);
        // "<b>" is only in the markup, "world" is in both
        Assert.Equal(["world", "<b>"], result.SearchTermsFoundInHtml);
        Assert.Equal(["world"], result.SearchTermsFoundInText);
    }

    [Fact]
    public async Task ScanAsync_without_search_terms_finds_nothing()
    {
        var result = await HtmlScanner.ScanAsync(Options(), PageUri.ToString(), "<body>text</body>", CancellationToken.None);

        Assert.False(result.IsScanError);
        Assert.Empty(result.SearchTermsFoundInHtml);
        Assert.Empty(result.SearchTermsFoundInText);
    }

    [Fact]
    public async Task ScanAsync_only_searches_include_selector()
    {
        var html = "<body><nav>menu</nav><main>content</main></body>";

        var result = await HtmlScanner.ScanAsync(Options(include: "main", terms: ["menu", "content"]), PageUri.ToString(), html, CancellationToken.None);

        Assert.Equal(["content"], result.SearchTermsFoundInText);
    }

    [Fact]
    public async Task ScanAsync_ignores_excluded_elements()
    {
        var html = "<body><main><p>content</p><aside class=\"ad\">sponsored</aside></main></body>";

        var result = await HtmlScanner.ScanAsync(Options(include: "main", exclude: ".ad", terms: ["content", "sponsored"]), PageUri.ToString(), html, CancellationToken.None);

        Assert.Equal(["content"], result.SearchTermsFoundInText);
        Assert.Equal(["content"], result.SearchTermsFoundInHtml);
    }

    [Fact]
    public async Task ScanAsync_exclude_selector_without_include_applies_to_body()
    {
        var html = "<body><p>content</p><footer>legal</footer></body>";

        var result = await HtmlScanner.ScanAsync(Options(exclude: "footer", terms: ["content", "legal"]), PageUri.ToString(), html, CancellationToken.None);

        Assert.Equal(["content"], result.SearchTermsFoundInText);
    }

    [Fact]
    public async Task ScanAsync_reports_error_when_include_selector_matches_nothing()
    {
        var result = await HtmlScanner.ScanAsync(Options(include: "#nope", terms: ["x"]), PageUri.ToString(), "<body>x</body>", CancellationToken.None);

        Assert.True(result.IsScanError);
        Assert.Empty(result.SearchTermsFoundInText);
    }

    [Fact]
    public async Task ScanAsync_reports_error_when_cancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await HtmlScanner.ScanAsync(Options(), PageUri.ToString(), "<body>x</body>", cts.Token);

        Assert.True(result.IsScanError);
    }

    [Fact]
    public async Task ScanAsync_finds_spider_links_only_when_enabled()
    {
        var html = "<body><a href=\"/about\">About</a></body>";

        var disabled = await HtmlScanner.ScanAsync(Options(spider: false), PageUri.ToString(), html, CancellationToken.None);
        var enabled = await HtmlScanner.ScanAsync(Options(spider: true), PageUri.ToString(), html, CancellationToken.None);

        Assert.Empty(disabled.SpiderLinks);
        Assert.Equal([new Uri("https://example.com/about")], enabled.SpiderLinks);
    }

    [Fact]
    public async Task ScanAsync_spider_links_are_local_normalized_http_links()
    {
        var html = """
            <body>
                <a href="/Relative/Page/">relative</a>
                <a href="https://example.com/absolute?q=1#frag">absolute</a>
                <a href="http://www.example.com/equivalent">equivalent domain</a>
                <a href="https://other.com/external">external</a>
                <a href="mailto:someone@example.com">mail</a>
                <a href="">empty</a>
                <a>no href</a>
            </body>
            """;

        var result = await HtmlScanner.ScanAsync(Options(spider: true), PageUri.ToString(), html, CancellationToken.None);

        Assert.Equal(
            [
                new Uri("https://example.com/relative/page"),
                new Uri("https://example.com/absolute"),
                new Uri("https://example.com/equivalent"),
            ],
            result.SpiderLinks);
    }

    [Fact]
    public async Task ScanAsync_spider_links_are_not_limited_by_include_selector()
    {
        var html = "<body><nav><a href=\"/nav-page\">nav</a></nav><main>content</main></body>";

        var result = await HtmlScanner.ScanAsync(Options(include: "main", spider: true), PageUri.ToString(), html, CancellationToken.None);

        Assert.Equal([new Uri("https://example.com/nav-page")], result.SpiderLinks);
    }
}
