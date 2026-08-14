using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration.Json;

var serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    WriteIndented = true
};

var builder = WebApplication.CreateBuilder(args);

// Machine-local overrides. This file is never shipped and never committed: an install copies
// over the top of the existing folder, so appsettings.json is replaced on every update while
// this one survives. The relative path resolves against the content root — the install
// directory in both interactive and service mode, since the host falls back to the app's base
// directory when the working directory is the one the service manager hands out.
//
// Slotted in behind the last file-based source rather than appended to the end, so it outranks
// every settings file while environment variables and command-line arguments still outrank it.
// Appending would have inverted that: the last source added wins over everything before it.
var configSources = builder.Configuration.Sources;
var lastFileSource = configSources.LastOrDefault(source => source is FileConfigurationSource);
configSources.Insert(
    lastFileSource is null ? configSources.Count : configSources.IndexOf(lastFileSource) + 1,
    new JsonConfigurationSource { Path = "appsettings.local.json", Optional = true });

builder.Host.UseWindowsService();
var app = builder.Build();

var userConfigPath = app.Configuration["BookmarkerConfigPath"] is { Length: > 0 } configured
    ? configured
    : @"C:\Program Files\Bookmarker\.bookmarker.json";

Defaults.EnsureUserConfig(userConfigPath, serializerOptions);

// Written next to the app at build time. Read once — the identity of a running build cannot
// change without restarting it.
var productionVersion = ProductionVersion.Read(
    [AppContext.BaseDirectory, app.Environment.ContentRootPath],
    serializerOptions);

var versionJsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

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

    return Render(template, pages!, userConfigPath, lastRead, productionVersion.FooterText);
}

string ErrorPage(string heading, string detail) => $"""
    <!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>bookmarker — error</title></head>
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

// Returns ProductionVersion.json verbatim when the build wrote one, so what a deployment reads
// is byte-for-byte what shipped. Falls back to the "unknown" identity when the file is absent.
app.MapGet("/api/diagnostics/version", () => productionVersion.RawJson is { Length: > 0 } raw
    ? Results.Content(raw, "application/json")
    : Results.Json(productionVersion, versionJsonOptions));

app.Run();

string Render(
    string htmlTemplate,
    IEnumerable<BookmarksTab> bookmarkPages,
    string configurationFileLocation,
    DateTime readAt,
    string versionText)
{
    var sb = new StringBuilder();
    foreach (var (bookmarkPage, pageIndex) in bookmarkPages.Select((v, i) => (v, i)))
    {
        sb.AppendLine($"<input type=\"radio\" id=\"{pageIndex}\" name=\"tabs\" {(pageIndex == 0 ? "checked" : "")}>");
        sb.AppendLine($"<label for=\"{pageIndex}\"> {bookmarkPage.Name} </label>");
        sb.AppendLine("<div>");

        foreach (var (bookmarkFileContent, fileIndex) in bookmarkPage.BookmarksFileContents.Select((v, i) => (v, i)))
        {

            sb.AppendLine($"<details open>");
            sb.AppendLine($"<summary><h2>{WebUtility.HtmlEncode(bookmarkFileContent.Name)}</h2></summary>");
            sb.AppendLine($"<div>");

            foreach (var (group, groupIndex) in bookmarkFileContent.Groups.Select((v, i) => (v, i)))
            {
                var useDetailsWrapper = !string.IsNullOrEmpty(group.Name);
                var isTable = string.Equals(group.Layout, "table", StringComparison.OrdinalIgnoreCase);

                if (useDetailsWrapper)
                {
                    sb.AppendLine($"<details open>");
                    sb.AppendLine($"<summary><h3>{WebUtility.HtmlEncode(group.Name)}</h3></summary>");
                }

                if (isTable)
                {
                    // One column per cell; widest row determines the track count. max-content
                    // tracks let the browser align columns exactly — no measurement needed.
                    var cols = group.Sets.Length == 0 ? 1 : group.Sets.Max(s => 1 + s.Bookmarks.Length);
                    sb.AppendLine($"<div class=\"grid-group\" style=\"--cols:{cols}\">");

                    foreach (var set in group.Sets)
                    {
                        sb.AppendLine("<div class=\"grid-row\">");

                        sb.AppendLine(
                            string.IsNullOrEmpty(set.Url)
                            ? $"<span class=\"set-label\">{WebUtility.HtmlEncode(set.Name)}</span>"
                            : $"<a href=\"{WebUtility.HtmlEncode(set.Url)}\" target=\"_blank\" rel=\"noreferrer\">{WebUtility.HtmlEncode(set.Name)}</a>");

                        foreach (var bookmark in set.Bookmarks)
                        {
                            sb.AppendLine($"<a href=\"{WebUtility.HtmlEncode(bookmark.Url)}\" target=\"_blank\" rel=\"noreferrer\">{WebUtility.HtmlEncode(bookmark.Name)}</a>");
                        }

                        sb.AppendLine("</div>");
                    }

                    sb.AppendLine("</div>");
                }
                else
                {
                    sb.AppendLine($"<div>");
                    sb.AppendLine($"<ul>");

                    foreach (var set in group.Sets)
                    {
                        sb.AppendLine("<li>");

                        sb.AppendLine(
                            string.IsNullOrEmpty(set.Url)
                            ? $"<span class=\"set-label\">{WebUtility.HtmlEncode(set.Name)}:</span>"
                            : $"<a href=\"{WebUtility.HtmlEncode(set.Url)}\" target=\"_blank\" rel=\"noreferrer\">{WebUtility.HtmlEncode(set.Name)}</a>");

                        foreach (var bookmark in set.Bookmarks)
                        {
                            sb.AppendLine($" | <a href=\"{WebUtility.HtmlEncode(bookmark.Url)}\" target=\"_blank\" rel=\"noreferrer\">{WebUtility.HtmlEncode(bookmark.Name)}</a>");
                        }

                        sb.AppendLine($"</li>");
                    }

                    sb.AppendLine($"</ul>");
                    sb.AppendLine($"</div>");
                }

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
        .Replace("{{timestamp}}", readAt.ToString("yyyy/MM/dd HH:mm"))
        .Replace("{{version}}", WebUtility.HtmlEncode(versionText));
}


