# LoadTest

This is a simple website load tester. It's not fully-featured and is just a toy/tool for myself, but feel free to use it.

Currently it's just source code with no releases, so you'll need the [.NET SDK](https://dot.net/download) to build and run it.

```powershell
# Load test using a remote site map for 30 seconds and limit the rate of requests.
dotnet run -- --mode sitemap --target-list 'https://developers.google.com/sitemap.xml' --seconds 30 --slow

# Use a local list of URLs rather than a site map.
# Force at least 20% chance of 404.
dotnet run -- --mode url-list --target-list './samples/sitemapUrls.txt' --chance-404 20 --slow
```

See the --help command for more.
