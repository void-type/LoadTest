# LoadTest

[![License](https://img.shields.io/github/license/void-type/LoadTest.svg)](https://github.com/void-type/LoadTest/blob/main/LICENSE.txt)
[![NuGet package](https://img.shields.io/nuget/v/Vt-loadtest.svg)](https://www.nuget.org/packages/vt-loadtest/)
[![MyGet package](https://img.shields.io/myget/voidcoredev/vpre/vt-loadtest.svg?label=myget)](https://www.myget.org/feed/voidcoredev/package/nuget/vt-loadtest)
[![Build Status](https://img.shields.io/azure-devops/build/void-type/VoidCore/22/main)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=22&branchName=main)
[![Test Coverage](https://img.shields.io/azure-devops/coverage/void-type/VoidCore/22/main)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=22&branchName=main)

[LoadTest](https://github.com/void-type/loadtest) is a simple website utility packaged in a .NET tool. It's not fully-featured and is just a toy/tool for myself, but feel free to use it.

- Load test a site using a list of URLs or point to the sitemap.
- Save a list of URLs from the sitemap.
- Archive html from pages.
  - Save html source or use --browser to save the JS-rendered page markup.
  - Use --log-browser-errors to capture JS console errors to QA your site.

## Install

You need the [.NET SDK](https://dot.net/download) to run this tool.

```powershell
dotnet tool install --global vt-loadtest

vt-loadtest --help
```

## Build

You need the [.NET SDK](https://dot.net/download) to build this project.

```powershell
./build/build.ps1
```

To install a local build:

```powershell
dotnet tool uninstall -g vt-loadtest
dotnet tool install -g vt-loadtest --add-source ./artifacts/dist/pre-release --prerelease
```

## Usage

If running from source, replace `vt-loadtest` with `dotnet run --`.

```powershell
# Load test using a remote sitemap on 2 threads. Works on nested sitemap indexes.
vt-loadtest run --path 'https://developers.google.com/tasks/sitemap.xml' --threads 2

# Load test using a remote sitemap index for 30 seconds and limit the rate of requests.
vt-loadtest run --path 'https://developers.google.com/sitemap.xml' --seconds 30 --delay

# Use a local list of URLs rather than a site map. Force at least 20% chance of 404.
vt-loadtest run --path './samples/sitemapUrls.txt' --chance-404 20

# Crawl a sitemap index and write the URLs to a local file to speed up repeat runs where sitemap retrieval is slow.
vt-loadtest make-list --path 'https://developers.google.com/sitemap.xml' --output './samples/url-list.txt'
```

See `vt-loadtest -h` for more.

## Known issues

URLs are technically case-sensitive, but some file systems aren't (Windows). This means when archiving pages, you may only get one page or the other in the event that the web server treats them as 2 different URLs. This should be rare as nearly identical URLs are bad for SEO.
