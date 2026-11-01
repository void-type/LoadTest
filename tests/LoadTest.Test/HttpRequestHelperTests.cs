namespace LoadTest.Test;

using LoadTest.Helpers;

public class HttpRequestHelperTests
{
    private static HttpRequestMessage NewRequest() => new(HttpMethod.Get, "https://example.com/");

    [Fact]
    public void ApplyHeaders_sets_user_agent_and_custom_headers()
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, ["X-Test: 1", "Accept-Language:  fr "], "MyAgent/1.0");

        Assert.Equal("MyAgent/1.0", request.Headers.UserAgent.ToString());
        Assert.Equal(["1"], request.Headers.GetValues("X-Test"));
        Assert.Equal(["fr"], request.Headers.GetValues("Accept-Language"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyHeaders_skips_blank_user_agent(string userAgent)
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, null, userAgent);

        Assert.Empty(request.Headers.UserAgent);
    }

    [Fact]
    public void ApplyHeaders_accepts_null_custom_headers()
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, null, null);

        Assert.Empty(request.Headers);
    }

    [Fact]
    public void ApplyHeaders_skips_headers_without_a_name()
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, ["no-separator", ": no-name", "X-Test: 1"], null);

        Assert.Equal(["X-Test"], request.Headers.Select(x => x.Key));
    }

    [Fact]
    public void ApplyHeaders_splits_on_first_colon_only()
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, ["Authorization: Bearer a:b:c"], null);

        Assert.Equal(["Bearer a:b:c"], request.Headers.GetValues("Authorization"));
    }

    [Fact]
    public void ApplyHeaders_does_not_validate_header_values()
    {
        using var request = NewRequest();

        HttpRequestHelper.ApplyHeaders(request, ["X-Custom: value with (odd) chars;"], null);

        Assert.Equal(["value with (odd) chars;"], request.Headers.GetValues("X-Custom"));
    }

    [Fact]
    public void GetExtraHeaders_returns_empty_for_null()
    {
        Assert.Empty(HttpRequestHelper.GetExtraHeaders(null));
    }

    [Fact]
    public void GetExtraHeaders_parses_headers()
    {
        var headers = HttpRequestHelper.GetExtraHeaders(["X-Test: 1", "Authorization:Bearer a:b", "no-separator", ": no-name"]);

        Assert.Equal(
            new Dictionary<string, string> { ["X-Test"] = "1", ["Authorization"] = "Bearer a:b" },
            headers);
    }

    [Fact]
    public void GetExtraHeaders_last_duplicate_wins()
    {
        var headers = HttpRequestHelper.GetExtraHeaders(["X-Test: 1", "X-Test: 2"]);

        Assert.Equal("2", Assert.Single(headers).Value);
    }
}
