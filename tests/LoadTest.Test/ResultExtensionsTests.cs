namespace LoadTest.Test;

using LoadTest.Helpers;
using LoadTest.Models;

public class ResultExtensionsTests
{
    private static PageArchiveResult ResultWith(params string[] urls) => new()
    {
        PageResults = [.. urls.Select(url => new PageArchivePageResult(new Uri(url)))],
    };

    [Fact]
    public void Combine_collection_concatenates_page_results_in_order()
    {
        var combined = new[] { ResultWith("https://a.com/1", "https://a.com/2"), ResultWith(), ResultWith("https://b.com/1") }.Combine();

        Assert.Equal(
            ["https://a.com/1", "https://a.com/2", "https://b.com/1"],
            combined.PageResults.Select(x => x.Url.ToString()));
    }

    [Fact]
    public void Combine_empty_collection_gives_empty_result()
    {
        Assert.Empty(Array.Empty<PageArchiveResult>().Combine().PageResults);
    }

    [Fact]
    public void Combine_does_not_modify_inputs()
    {
        var first = ResultWith("https://a.com/1");
        var second = ResultWith("https://b.com/1");

        var combined = first.Combine(second);

        Assert.Single(first.PageResults);
        Assert.Single(second.PageResults);
        Assert.Equal(2, combined.PageResults.Count);
    }

    [Fact]
    public void Combine_adds_to_existing_results_of_the_receiver()
    {
        var combined = ResultWith("https://a.com/1").Combine([ResultWith("https://b.com/1"), ResultWith("https://c.com/1")]);

        Assert.Equal(3, combined.RequestCount);
    }

    [Fact]
    public void Result_counts_are_derived_from_page_results()
    {
        var result = new PageArchiveResult
        {
            PageResults =
            [
                new PageArchivePageResult(new Uri("https://a.com/1")),
                new PageArchivePageResult(new Uri("https://a.com/2")) { IsRetrieveError = true },
                new PageArchivePageResult(new Uri("https://a.com/3")) { IsScanError = true },
                new PageArchivePageResult(new Uri("https://a.com/4")) { IsError = true },
                new PageArchivePageResult(new Uri("https://a.com/5")) { IsError = true, IsRetrieveError = true },
            ],
        };

        Assert.Equal(5, result.RequestCount);
        Assert.Equal(2, result.RetrieveErrorCount);
        Assert.Equal(1, result.ScanErrorCount);
        Assert.Equal(2, result.OtherErrorCount);
    }

    [Theory]
    [InlineData(199, false)]
    [InlineData(200, true)]
    [InlineData(204, true)]
    [InlineData(299, true)]
    [InlineData(300, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public void IsSuccessStatusCode_covers_only_2xx(int statusCode, bool expected)
    {
        using var response = new HttpResponseMessage((System.Net.HttpStatusCode)statusCode);

        // Called statically because HttpResponseMessage's own instance property would win otherwise.
        Assert.Equal(expected, ResponseExtensions.IsSuccessStatusCode(response));
    }
}
