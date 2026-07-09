using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using LoadTest.Models;
using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class HtmlScanner
{
    /// <summary>
    /// Finds embedded resource links (img, stylesheet, and script) in HTML content, resolved against the page URI.
    /// Only resources on the page's own domain or one of <paramref name="allowedResourceDomains"/> are included.
    /// </summary>
    public static async Task<List<Uri>> FindResourceLinksAsync(Uri pageUri, string htmlContent, string[]? allowedResourceDomains, CancellationToken cancellationToken)
    {
        var parser = new HtmlParser();
        var doc = await parser.ParseDocumentAsync(htmlContent, cancellationToken);

        var elements = doc?.QuerySelectorAll("img[src], link[href], script[src]")
            ?? Enumerable.Empty<IElement>();

        var links = new List<Uri>();

        foreach (var element in elements)
        {
            var isLink = element.LocalName == "link";

            // The CSS attribute selector can't match rel case-insensitively or handle multi-token
            // values like rel="stylesheet preload", so filter link elements here.
            if (isLink && !IsResourceLink(element))
            {
                continue;
            }

            var attributeName = isLink ? "href" : "src";
            var value = element.GetAttribute(attributeName);

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!Uri.TryCreate(pageUri, value, out var resourceUri) ||
                (resourceUri.Scheme != Uri.UriSchemeHttp && resourceUri.Scheme != Uri.UriSchemeHttps))
            {
                continue;
            }

            var isAllowedDomain = resourceUri.Host.EqualsIgnoreCase(pageUri.Host) ||
                (allowedResourceDomains?.Any(domain => resourceUri.Host.EqualsIgnoreCase(domain)) ?? false);

            if (isAllowedDomain)
            {
                links.Add(resourceUri);
            }
        }

        return links.Distinct().ToList();
    }

    // Preloaded assets are fetched by a real browser even if they're never applied, so they count as load.
    private static readonly string[] PreloadResourceTypes = ["style", "script", "image", "font"];

    private static bool IsResourceLink(IElement linkElement)
    {
        // rel is a space-separated token list, e.g. "stylesheet preload".
        var relTokens = (linkElement.GetAttribute("rel") ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (relTokens.Any(token => token.EqualsIgnoreCase("stylesheet")))
        {
            return true;
        }

        if (relTokens.Any(token => token.EqualsIgnoreCase("preload")))
        {
            var asValue = linkElement.GetAttribute("as");
            return asValue is not null && PreloadResourceTypes.Any(type => type.EqualsIgnoreCase(asValue));
        }

        return false;
    }

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
