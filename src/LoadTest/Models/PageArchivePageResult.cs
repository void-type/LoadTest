namespace LoadTest.Models;

public class PageArchivePageResult
{
    public PageArchivePageResult(Uri uri)
    {
        Url = uri;
    }

    public Uri Url { get; }

    public Uri? FinalUrl { get; set; }

    public bool IsRedirected { get; set; }

    public bool IsCrossDomainRedirect { get; set; }

    public int StatusCode { get; set; }

    public bool IsError { get; set; }

    public bool IsRetrieveError { get; set; }

    public bool HtmlSaved { get; set; }

    public bool IsOnlyFoundBySpider { get; set; }

    public bool IsScanError { get; set; }

    public List<Uri> SpiderLinks { get; set; } = [];

    public bool WasSearchTermsFound { get; set; }

    public List<string> SearchTermsFoundInHtml { get; set; } = [];

    public List<string> SearchTermsFoundInText { get; set; } = [];
}
