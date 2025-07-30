using Cocona;
using LoadTest;
using LoadTest.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = CoconaApp.CreateBuilder();
var services = builder.Services;

// Disable logging
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddFilter(_ => false);
});

services.AddHttpClient();
services.AddSingleton<UrlsRetriever>();
services.AddSingleton<LoadTester>();
services.AddSingleton<PageArchiver>();
services.AddSingleton<HtmlContentRetriever>();

var app = builder.Build();

app.AddCommands<LoadTestCommands>();

await app.RunAsync();
