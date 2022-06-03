# LoadTest

This is a simple website load tester. It's not fully-featured and is just a toy/tool for myself, but feel free to use it.

Currently it's just source code with no releases, so you'll need the [.NET SDK](https://dot.net/download) to build and run it.

```powershell
# Load test using a remote site map for 30 seconds.
dotnet run -- --mode sitemap-url --target-list 'https://localhost:5001/sitemaps/sitemap.xml' --seconds 30

# Use a local list of URLs rather than a site map.
# Force at least 20% chance of 404 and limit the rate of requests.
dotnet run -- --mode url-list-file --target-list './samples/sitemapUrls.txt' --chance-404 20 --slow
```

See the --help command for more.
