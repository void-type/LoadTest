namespace LoadTest.Models;

public class PageScanResult
{
    public bool IsScanError { get; set; }
    public List<string> SearchTermsFoundInHtml { get; set; } = [];
    public List<string> SearchTermsFoundInText { get; set; } = [];
    public List<Uri> SpiderLinks { get; set; } = [];
}
