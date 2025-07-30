using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using LoadTest.Models;
using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class HtmlScanner
{
    public static async Task<PageScanResult> ScanAsync(PageArchiveOptions options, string pageUrl, string htmlContent, CancellationToken cancellationToken)
    {
        var pageResult = new PageScanResult();

        try
        {
            // Gets the body element from the HTML content, using the API of the AngleSharp library
            var parser = new HtmlParser();
            var doc = await parser.ParseDocumentAsync(htmlContent, cancellationToken);
            var body = doc?.Body;

            // Only select DOM from main element
            // If setting is empty, we'll just keep using body
            var main = (string.IsNullOrWhiteSpace(options.ContentIncludeSelector) ?
                body :
                doc?.QuerySelector(options.ContentIncludeSelector))
                ?? throw new InvalidOperationException($"Could not find main content element using selector {options.ContentIncludeSelector}");

            if (!string.IsNullOrWhiteSpace(options.ContentExcludeSelector))
            {
                // Removes any elements we don't want
                foreach (var element in main.QuerySelectorAll(options.ContentExcludeSelector))
                {
                    element.Remove();
                }
            }

            // Look for the page html contains the keyword
            pageResult.SearchTermsFoundInHtml.AddRange(options.ContentSearchTerms.WhereFoundIn(main.OuterHtml));

            // Look for the page text contains the keyword
            pageResult.SearchTermsFoundInText.AddRange(options.ContentSearchTerms.WhereFoundIn(main.TextContent));

            if (options.IsSpiderEnabled)
            {
                // Look for local URLs to spider, check the whole page.
                pageResult.SpiderLinks.AddRange(FindSpiderLinks(options, pageUrl, body));
            }
        }
        catch (OperationCanceledException)
        {
            pageResult.IsScanError = true;
            return pageResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scanning HTML content for {pageUrl}: {ex.Message}");
            pageResult.IsScanError = true;
        }

        return pageResult;
    }

    private static List<Uri> FindSpiderLinks(PageArchiveOptions options, string pageUrl, IHtmlElement? body)
    {
        var spiderLinks = new List<Uri>();

        var anchorElements = body?.QuerySelectorAll("a[href]");

        if (anchorElements is not null)
        {
            foreach (var element in anchorElements)
            {
                var href = element.GetAttribute("href");

                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                var hrefUri = href.GetNormalizedUri(options.PrimaryDomain, options.PrimaryDomainEquivalents, pageUrl, quiet: true);

                if (hrefUri is null)
                {
                    continue;
                }

                // Ignore external links
                if (!hrefUri.Host.EqualsIgnoreCase(options.PrimaryDomain))
                {
                    continue;
                }

                // Ignore other schemes like mailto
                if (hrefUri.Scheme != "http" && hrefUri.Scheme != "https")
                {
                    continue;
                }

                // Strip query string and fragments
                spiderLinks.Add(hrefUri);
            }
        }

        return spiderLinks;
    }
}
