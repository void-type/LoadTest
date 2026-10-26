using System.IO.Compression;
using System.Text;

namespace LoadTest.Helpers;

public static class SitemapHelpers
{
    /// <summary>
    /// If the path is an HTTP(S) root domain (with or without a trailing slash), returns the URL of its robots.txt.
    /// </summary>
    public static Uri? GetRobotsUri(string path)
    {
        if (!Uri.TryCreate(path.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        if (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return null;
        }

        return new Uri(uri, "/robots.txt");
    }

    /// <summary>
    /// Get the sitemap URLs declared in the content of a robots.txt.
    /// </summary>
    public static string[] GetSitemapUrlsFromRobots(string robotsContent)
    {
        const string directive = "sitemap:";

        return robotsContent
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[directive.Length..].Trim())
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Decode sitemap content to a string, decompressing first if it is gzipped (such as sitemap.xml.gz).
    /// </summary>
    public static async Task<string> DecodeSitemapAsync(byte[] content, CancellationToken cancellationToken)
    {
        await using var raw = new MemoryStream(content);

        if (!IsGzip(content))
        {
            using var rawReader = new StreamReader(raw, Encoding.UTF8);
            return await rawReader.ReadToEndAsync(cancellationToken);
        }

        await using var gzip = new GZipStream(raw, CompressionMode.Decompress);
        using var gzipReader = new StreamReader(gzip, Encoding.UTF8);

        return await gzipReader.ReadToEndAsync(cancellationToken);
    }

    private static bool IsGzip(byte[] content)
    {
        return content.Length > 2 && content[0] == 0x1f && content[1] == 0x8b;
    }
}
