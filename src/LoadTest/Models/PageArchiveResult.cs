namespace LoadTest.Models;

public class PageArchiveResult
{
    public long RequestCount => PageResults.Count;

    public long RetrieveErrorCount => PageResults.Count(x => x.IsRetrieveError);

    public long ScanErrorCount => PageResults.Count(x => x.IsScanError);

    public long OtherErrorCount => PageResults.Count(x => x.IsError);

    public List<PageArchivePageResult> PageResults { get; set; } = [];
}
