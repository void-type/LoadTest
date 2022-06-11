# LoadTest

This is a simple website load tester. It's not fully-featured and is just a toy/tool for myself, but feel free to use it.

You can use the [.NET SDK](https://dot.net/download) to build and run the source or download one of the [releases](https://github.com/void-type/LoadTest/releases) and [install it as a .NET tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use#use-the-tool-as-a-global-tool).

If using the SDK, replace `load-test` with `dotnet run --`.

```powershell
# Load test using a remote sitemap on 2 threads. Works on nested sitemap indexes.
load-test run --path 'https://developers.google.com/tasks/sitemap.xml' --threads 2

# Load test using a remote sitemap index for 30 seconds and limit the rate of requests.
load-test run --path 'https://developers.google.com/sitemap.xml' --seconds 30 --delay

# Use a local list of URLs rather than a site map. Force at least 20% chance of 404.
load-test run --path './samples/sitemapUrls.txt' --chance-404 20

# Crawl a sitemap index and write the URLs to a local file to speed up repeat runs where sitemap retrieval is slow.
load-test make-list --path 'https://developers.google.com/sitemap.xml' --output './samples/url-list.txt'
```

See the `--help` for more.
