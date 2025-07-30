namespace LoadTest.Models;

public class HtmlContentRetrieverResult
{
    public Uri? FinalUrl { get; set; }

    public int StatusCode { get; set; }

    public bool IsRetrieveError { get; set; }

    public string HtmlContent { get; set; } = string.Empty;
}
