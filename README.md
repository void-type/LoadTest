# LoadTest

```powershell
# Load test using a remote site map for 30 seconds.
dotnet run -- --mode sitemap-url --target-list 'https://ball.com/sitemaps/sitemap.xml' --seconds 30

# Use a local list of URLs rather than a site map.
# Force at least 20% chance of 404 and limit the rate of requests.
dotnet run -- --mode url-list-file --target-list './samples/sitemapUrls.txt' --chance-404 20 --slow
```
