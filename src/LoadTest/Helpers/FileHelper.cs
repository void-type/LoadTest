using CsvHelper;
using LoadTest.Csv;
using LoadTest.Models;
using System.Globalization;
using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class FileHelper
{
    public static async Task SaveHtmlContentAsync(PageArchiveOptions options, Uri uri, string content, CancellationToken cancellationToken)
    {
        var htmlFileFolder = GetHtmlFileFolder(options, uri);
        var htmlFileName = GetHtmlFileName(uri);
        var htmlFilePath = Path.Combine(htmlFileFolder, htmlFileName);

        if (options.IsVerbose)
        {
            Console.WriteLine($"Writing {content.Length} chars to {htmlFilePath}");
        }

        Directory.CreateDirectory(htmlFileFolder);
        await File.WriteAllTextAsync(htmlFilePath, content, cancellationToken);
    }

    public static string GetHtmlFileFolder(PageArchiveOptions config, Uri uri)
    {
        return Path.Combine(
            config.OutputPath,
            "html",
            uri.Host,
            string.Concat(uri.Segments[..^1]).TrimStart('/'))
        .GetSafeFilePath();
    }

    public static string GetHtmlFileName(Uri uri)
    {
        var lastUriSegment = uri.Segments[^1];

        if (lastUriSegment.IsNullOrWhiteSpace() || lastUriSegment == "/")
        {
            lastUriSegment = "index";
        }

        lastUriSegment = lastUriSegment.TrimEnd('/');

        return (lastUriSegment + ".html").GetSafeFileName();
    }

    public static async Task SaveResultsCsvAsync(string csvFilePath, PageArchiveResult jobResult)
    {
        await using var writer = new StreamWriter(csvFilePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.TypeConverterCache.AddConverter<List<string>>(new StringListConverter());
        csv.Context.TypeConverterCache.AddConverter<List<Uri>>(new UriListConverter());
        await csv.WriteRecordsAsync(jobResult.PageResults);
    }
}
