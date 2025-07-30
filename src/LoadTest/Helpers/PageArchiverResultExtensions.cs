using LoadTest.Models;

namespace LoadTest.Helpers;

public static class PageArchiverResultExtensions
{
    public static PageArchiveResult Combine(this IEnumerable<PageArchiveResult> metricCollection) =>
        new PageArchiveResult().Combine(metricCollection);

    public static PageArchiveResult Combine(this PageArchiveResult metrics, IEnumerable<PageArchiveResult> metricCollection) =>
        metricCollection.Aggregate(metrics, (acc, x) => acc.Combine(x));

    public static PageArchiveResult Combine(this PageArchiveResult metrics, PageArchiveResult newMetrics) => new()
    {
        PageResults = [.. metrics.PageResults, .. newMetrics.PageResults],
    };
}
