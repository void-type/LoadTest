namespace LoadTest.Test;

using LoadTest.Helpers;

public class UriHelpersTests
{
    [Theory]
    // Add root slash
    [InlineData("https://example.com", "https://example.com/")]
    // Relative Protocol
    [InlineData("//example.com", "https://example.com/")]
    // Https upgrade
    [InlineData("http://example.com/", "https://example.com/")]
    // Lowercase domain
    [InlineData("https://EXAMPLE.com/", "https://example.com/")]
    // Lowercase path
    [InlineData("https://example.com/TEST/TEST/", "https://example.com/test/test")]
    // Relative URL
    [InlineData("/", "https://example.com/")]
    [InlineData("/TEST/TEST/", "https://example.com/test/test")]
    // Special characters are not encoded
    [InlineData("/TE-&= ST", "https://example.com/te-&= st")]
    // Fragments and queries are removed
    [InlineData("/TEST/?q=test#something", "https://example.com/test")]
    [InlineData("/TEST/#something", "https://example.com/test")]
    [InlineData("/TEST/?q=test", "https://example.com/test")]
    // Empty returns null
    [InlineData("", null)]
    // External domain
    [InlineData("notexample.com", "https://notexample.com/")]
    public void GetNormalizedUri_protocol_domain_and_path(string url, string expectedNormalizedUrl)
    {
        Assert.Equal(expectedNormalizedUrl, url.GetNormalizedUri("EXAMPLE.com", [], null)?.ToString());
    }

    [Theory]
    [InlineData("example.com", "https://example.com/TEST/TEST/", "https://example.com/test/test")]
    [InlineData("example.com", "https://exam.ple/TEST/TEST/", "https://example.com/test/test")]
    [InlineData("example.com", "https://www.example.com/TEST/TEST/", "https://example.com/test/test")]
    [InlineData("example.com", "http://www.example.com", "https://example.com/")]
    [InlineData("example.com", "//www.example.com", "https://example.com/")]
    [InlineData("example.com", "/", "https://example.com/")]
    public void GetNormalizedUri_primary_domain_equivalents_1(string primaryDomain, string url, string expectedNormalizedUrl)
    {
        var equivalents = new string[] { "www.example.com", "exam.ple" };

        Assert.Equal(expectedNormalizedUrl, url.GetNormalizedUri(primaryDomain, equivalents, null)?.ToString());
    }

    [Theory]
    [InlineData("www.example.com", "https://www.example.com/TEST/TEST/", "https://www.example.com/test/test")]
    [InlineData("www.example.com", "https://exam.ple/TEST/TEST/", "https://www.example.com/test/test")]
    [InlineData("www.example.com", "https://example.com/TEST/TEST/", "https://www.example.com/test/test")]
    [InlineData("www.example.com", "http://example.com", "https://www.example.com/")]
    [InlineData("www.example.com", "//example.com", "https://www.example.com/")]
    [InlineData("www.example.com", "/", "https://www.example.com/")]
    [InlineData("www.example.com", "notexample.com", "https://notexample.com/")]
    public void GetNormalizedUri_primary_domain_equivalents_2(string primaryDomain, string url, string expectedNormalizedUrl)
    {
        var equivalents = new string[] { "example.com", "exam.ple" };

        Assert.Equal(expectedNormalizedUrl, url.GetNormalizedUri(primaryDomain, equivalents, null)?.ToString());
    }

    [Theory]
    [InlineData("www.example.com", "/TEST/TEST/", null, "https://www.example.com/test/test")]
    [InlineData("www.example.com", "~/TEST/TEST/", null, "https://www.example.com/test/test")]
    [InlineData("www.example.com", "/TEST/TEST/", "https://sample.com", "https://sample.com/test/test")]
    [InlineData("www.example.com", "~/TEST/TEST/", "https://sample.com", "https://sample.com/test/test")]
    [InlineData(null, "~/TEST/TEST/", null, null)]
    public void GetNormalizedUri_relative_domain_resolution(string primaryDomain, string url, string foundAt, string expectedNormalizedUrl)
    {
        var equivalents = new string[] { "example.com", "exam.ple" };

        Assert.Equal(expectedNormalizedUrl, url.GetNormalizedUri(primaryDomain, equivalents, foundAt)?.ToString());
    }
}
