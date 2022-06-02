# $siteMapUrl = "https://localhost:5001/sitemaps/sitemap.xml"
# [xml]$siteMap = (Invoke-WebRequest -Uri $siteMapUrl -UseBasicParsing).Content

[xml]$siteMap = Get-Content -Path ./sitemap.ball.xml -Raw

[string[]]$urls = @()

foreach ($url in $siteMap.urlset.url) {
  $urls += $url.loc;
  foreach ($alt in $url['xhtml:link']) {
    $urls += $alt.href;
  }
}

$urls | Sort-Object | Get-Unique
