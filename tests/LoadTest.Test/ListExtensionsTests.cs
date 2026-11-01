namespace LoadTest.Test;

using LoadTest.Helpers;

public class ListExtensionsTests
{
    [Theory]
    [InlineData("Hello World", true)]
    [InlineData("nothing here", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void AnyFoundIn_checks_text(string text, bool expected)
    {
        List<string> keywords = ["world", "foo"];

        Assert.Equal(expected, keywords.AnyFoundIn(text));
    }

    [Fact]
    public void AnyFoundIn_is_false_for_null_or_empty_keywords()
    {
        List<string> nullKeywords = null;

        Assert.False(nullKeywords.AnyFoundIn("text"));
        Assert.False(new List<string>().AnyFoundIn("text"));
    }

    [Fact]
    public void AnyFoundIn_respects_comparison_type()
    {
        List<string> keywords = ["world"];

        Assert.True(keywords.AnyFoundIn("Hello WORLD"));
        Assert.False(keywords.AnyFoundIn("Hello WORLD", StringComparison.Ordinal));
    }

    [Fact]
    public void WhereFoundIn_returns_matching_keywords_in_keyword_order()
    {
        List<string> keywords = ["zebra", "apple", "missing", "Banana"];

        Assert.Equal(["zebra", "Banana"], keywords.WhereFoundIn("a BANANA and a ZEBRA"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void WhereFoundIn_returns_empty_for_blank_text(string text)
    {
        List<string> keywords = ["foo"];

        Assert.Empty(keywords.WhereFoundIn(text));
    }

    [Fact]
    public void WhereFoundIn_returns_empty_for_null_or_empty_keywords()
    {
        List<string> nullKeywords = null;

        Assert.Empty(nullKeywords.WhereFoundIn("text"));
        Assert.Empty(new List<string>().WhereFoundIn("text"));
    }

    [Fact]
    public void WhereFoundIn_respects_comparison_type()
    {
        List<string> keywords = ["world"];

        Assert.Empty(keywords.WhereFoundIn("Hello WORLD", StringComparison.Ordinal));
    }
}
