namespace LoadTest.Test;

using LoadTest.Helpers;
using LoadTest.Models;

public sealed class FileHelperTests : IDisposable
{
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), "LoadTestTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, recursive: true);
        }
    }

    private PageArchiveOptions Options() => new() { OutputPath = _outputPath };

    [Theory]
    [InlineData("https://example.com/", "index.html")]
    [InlineData("https://example.com", "index.html")]
    [InlineData("https://example.com/post", "post.html")]
    [InlineData("https://example.com/blog/post", "post.html")]
    // Trailing slash is dropped
    [InlineData("https://example.com/blog/", "blog.html")]
    [InlineData("https://example.com/blog/post/", "post.html")]
    public void GetHtmlFileName_uses_last_segment(string url, string expected)
    {
        Assert.Equal(expected, FileHelper.GetHtmlFileName(new Uri(url)));
    }

    [Fact]
    public void GetHtmlFileName_replaces_characters_unsafe_for_file_names()
    {
        var fileName = FileHelper.GetHtmlFileName(new Uri("https://example.com/a:b*c"));

        Assert.EndsWith(".html", fileName);
        Assert.Equal(-1, fileName.IndexOfAny(Path.GetInvalidFileNameChars()));
    }

    [Theory]
    [InlineData("https://example.com/", new string[] { "html", "example.com" })]
    [InlineData("https://example.com/post", new string[] { "html", "example.com" })]
    [InlineData("https://example.com/blog/post", new string[] { "html", "example.com", "blog" })]
    [InlineData("https://example.com/a/b/c/post", new string[] { "html", "example.com", "a", "b", "c" })]
    public void GetHtmlFileFolder_is_output_html_host_and_parent_segments(string url, string[] expectedParts)
    {
        var folder = FileHelper.GetHtmlFileFolder(Options(), new Uri(url));

        Assert.Equal(
            Path.GetFullPath(Path.Combine([_outputPath, .. expectedParts])),
            Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public void GetHtmlFileFolder_separates_hosts()
    {
        var a = FileHelper.GetHtmlFileFolder(Options(), new Uri("https://a.example.com/page"));
        var b = FileHelper.GetHtmlFileFolder(Options(), new Uri("https://b.example.com/page"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task SaveHtmlContentAsync_writes_file_and_creates_folders()
    {
        var uri = new Uri("https://example.com/blog/post");

        await FileHelper.SaveHtmlContentAsync(Options(), uri, "<html>hi</html>", CancellationToken.None);

        var expectedPath = Path.Combine(_outputPath, "html", "example.com", "blog", "post.html");
        Assert.Equal("<html>hi</html>", await File.ReadAllTextAsync(expectedPath));
    }

    [Fact]
    public async Task SaveHtmlContentAsync_overwrites_existing_file()
    {
        var uri = new Uri("https://example.com/");

        await FileHelper.SaveHtmlContentAsync(Options(), uri, "first", CancellationToken.None);
        await FileHelper.SaveHtmlContentAsync(Options(), uri, "second", CancellationToken.None);

        Assert.Equal("second", await File.ReadAllTextAsync(Path.Combine(_outputPath, "html", "example.com", "index.html")));
    }

    [Fact]
    public async Task SaveResultsCsvAsync_writes_a_row_per_page_with_lists_joined()
    {
        var csvPath = Path.Combine(_outputPath, "nested", "results.csv");
        var result = new PageArchiveResult
        {
            PageResults =
            [
                new PageArchivePageResult(new Uri("https://example.com/one"))
                {
                    StatusCode = 200,
                    HtmlSaved = true,
                    SearchTermsFoundInHtml = ["alpha", "beta"],
                    SpiderLinks = [new Uri("https://example.com/a"), new Uri("https://example.com/b")],
                },
                new PageArchivePageResult(new Uri("https://example.com/two")) { StatusCode = 404, IsError = true },
            ],
        };

        await FileHelper.SaveResultsCsvAsync(csvPath, result);

        var lines = await File.ReadAllLinesAsync(csvPath);

        Assert.Equal(3, lines.Length);
        Assert.Contains("Url", lines[0]);
        Assert.Contains("SearchTermsFoundInHtml", lines[0]);
        Assert.Contains("https://example.com/one", lines[1]);
        Assert.Contains("alpha; beta", lines[1]);
        Assert.Contains("https://example.com/a; https://example.com/b", lines[1]);
        Assert.Contains("https://example.com/two", lines[2]);
        Assert.Contains("404", lines[2]);
    }

    [Fact]
    public async Task SaveResultsCsvAsync_with_no_results_still_creates_file()
    {
        var csvPath = Path.Combine(_outputPath, "empty.csv");

        await FileHelper.SaveResultsCsvAsync(csvPath, new PageArchiveResult());

        Assert.True(File.Exists(csvPath));
    }
}
