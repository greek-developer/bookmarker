using System.Net;
using System.Text;
using System.Text.Json;

var serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    WriteIndented = true
};

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
var app = builder.Build();

var userConfigPath = app.Configuration["BookmarkerConfigPath"] is { Length: > 0 } configured
    ? configured
    : @"C:\Program Files\Bookmarker\.bookmarker.json";

Defaults.EnsureUserConfig(userConfigPath, serializerOptions);

string? cachedHtml = null;
DateTime lastRead = DateTime.MinValue;

string BuildHtml(IHostEnvironment host)
{
    lastRead = DateTime.Now;

    var template = File.ReadAllText(Path.Combine(host.ContentRootPath, "template.html"));

    BookmarkerOptions? bookmarkerOptions;
    try
    {
        bookmarkerOptions = JsonSerializer.Deserialize<BookmarkerOptions>(
            File.ReadAllText(userConfigPath),
            serializerOptions);
    }
    catch (Exception ex)
    {
        return ErrorPage($"Failed to parse config file: {userConfigPath}", ex.Message);
    }

    var pages = bookmarkerOptions?.Tabs.Select(pageOptions =>
    {
        var contents = new List<BookmarksFileContent>();
        foreach (var file in pageOptions.Files.Where(File.Exists))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<BookmarksFileContent>(File.ReadAllText(file), serializerOptions);
                if (parsed is not null) contents.Add(parsed);
            }
            catch (Exception ex)
            {
                contents.Add(new BookmarksFileContent { Name = $"[parse error] {Path.GetFileName(file)}: {ex.Message}" });
            }
        }
        return new BookmarksTab { Name = pageOptions.Name, BookmarksFileContents = contents.ToArray() };
    }).ToArray();

    return Render(template, pages!, userConfigPath, lastRead);
}

string ErrorPage(string heading, string detail) => $"""
    <!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>bookmarker — error</title>
    <style>body{{background:#16181d;color:#e3e6ec;font-family:ui-monospace,monospace;padding:40px}}
    h1{{color:#3fd7d6;font-size:16px}}pre{{color:#9aa0ac;white-space:pre-wrap;word-break:break-all}}</style></head>
    <body><h1>bookmarker</h1><p>{WebUtility.HtmlEncode(heading)}</p><pre>{WebUtility.HtmlEncode(detail)}</pre></body></html>
    """;

app.MapGet("/", (IHostEnvironment host) =>
{
    cachedHtml ??= BuildHtml(host);
    return Results.Content(cachedHtml, "text/html");
});

app.MapPost("/refresh", (IHostEnvironment host) =>
{
    cachedHtml = null;
    return Results.Redirect("/");
});

app.MapGet("/refresh", () => { cachedHtml = null; return Results.Redirect("/"); });

app.Run();

string Render(
    string htmlTemplate,
    IEnumerable<BookmarksTab> bookmarkPages,
    string configurationFileLocation,
    DateTime readAt)
{
    var sb = new StringBuilder();
    foreach(var (bookmarkPage, pageIndex) in bookmarkPages.Select((v,i) => (v,i)))
    {
        sb.AppendLine($"<input type=\"radio\" id=\"{pageIndex}\" name=\"tabs\" {(pageIndex == 0 ? "checked" : "")}>");
        sb.AppendLine($"<label for=\"{pageIndex}\"> {bookmarkPage.Name} </label>");
        sb.AppendLine("<div>");

        foreach(var (bookmarkFileContent, fileIndex) in bookmarkPage.BookmarksFileContents.Select((v,i) => (v,i)))
        {

            sb.AppendLine($"<details open>");
            sb.AppendLine($"<summary><h2>{WebUtility.HtmlEncode(bookmarkFileContent.Name)}</h2></summary>");
            sb.AppendLine($"<div>");

            foreach(var (group, groupIndex) in bookmarkFileContent.Groups.Select((v,i) => (v,i)))
            {
                var useDetailsWrapper = !string.IsNullOrEmpty(group.Name);

                if (useDetailsWrapper)
                {
                    sb.AppendLine($"<details open>");
                    sb.AppendLine($"<summary><h3>{WebUtility.HtmlEncode(group.Name)}</h3></summary>");
                }

                sb.AppendLine($"<div>");
                sb.AppendLine($"<ul>");

                foreach(var set in group.Sets)
                {
                    sb.AppendLine("<li>");

                    sb.AppendLine(
                        string.IsNullOrEmpty(set.Url)
                        ? $"<span class=\"set-label\">{WebUtility.HtmlEncode(set.Name)}:</span>"
                        : $"<a href=\"{WebUtility.HtmlEncode(set.Url)}\" target=\"_blank\">{WebUtility.HtmlEncode(set.Name)}</a>");

                    foreach(var bookmark in set.Bookmarks)
                    {
                        sb.AppendLine($" | <a href=\"{WebUtility.HtmlEncode(bookmark.Url)}\" target=\"_blank\">{WebUtility.HtmlEncode(bookmark.Name)}</a>");
                    }

                    sb.AppendLine($"</li>");
                }

                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
                
                if (useDetailsWrapper)
                {
                    sb.AppendLine($"</details>");
                }
            }            
            sb.AppendLine($"</div>");
            sb.AppendLine($"</details>");
        }
        sb.AppendLine("</div>");
    }

    var content = sb.ToString();

    return htmlTemplate
        .Replace("{{content}}", content)
        .Replace("{{footer}}", configurationFileLocation.ToLower())
        .Replace("{{timestamp}}", readAt.ToString("yyyy/MM/dd HH:mm"));
}


