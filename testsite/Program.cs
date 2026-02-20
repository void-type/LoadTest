var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var baseUrl = "https://localhost:5111";

var pages = new Dictionary<string, string>
{
    ["/"] = "Home",
    ["/about"] = "About",
    ["/contact"] = "Contact",
    ["/products"] = "Products",
    ["/faq"] = "FAQ",
};

app.MapGet("/sitemap.xml", (HttpContext context) =>
{
    LogHeaders(context);

    var urls = string.Join("\n", pages.Keys.Select(path =>
        $"  <url><loc>{baseUrl}{path}</loc></url>"));

    var xml = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
        {urls}
        </urlset>
        """;

    return Results.Content(xml, "application/xml");
});

foreach (var (path, title) in pages)
{
    app.MapGet(path, (HttpContext context) =>
    {
        LogHeaders(context);

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head><title>{title}</title></head>
            <body>
                <h1>{title}</h1>
                <p>This is the {title} page.</p>
                <nav>
                    {string.Join("\n            ", pages.Select(p => $"<a href=\"{p.Key}\">{p.Value}</a>"))}
                </nav>
            </body>
            </html>
            """;

        return Results.Content(html, "text/html");
    });
}

app.Run(baseUrl);

static void LogHeaders(HttpContext context)
{
    Console.WriteLine($"\n--- {context.Request.Method} {context.Request.Path} ---");

    foreach (var header in context.Request.Headers.OrderBy(h => h.Key))
    {
        Console.WriteLine($"  {header.Key}: {header.Value}");
    }
}
