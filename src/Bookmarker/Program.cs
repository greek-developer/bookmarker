using System.Text;
using System.Text.Json;

var serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    WriteIndented = true
};

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var userConfigPath = Path.Combine(userProfile, ".bookmarker.json");

if (!File.Exists(userConfigPath))
{
    Console.WriteLine($"No configuration file found at {userConfigPath}, creating defaults.");

    var welcomeFilePath = Path.Combine(userProfile, ".bookmarker.welcome.json");

    var defaultOptions = new BookmarkerOptions
    {
        Tabs = new[]
        {
            new BookmarkerTabsOptions { Name = "Welcome", Files = new[] { welcomeFilePath } }
        }
    };

    File.WriteAllText(userConfigPath, JsonSerializer.Serialize(defaultOptions, serializerOptions));
    File.WriteAllText(welcomeFilePath, JsonSerializer.Serialize(WelcomeBookmarkSet().BookmarksFileContents[0], serializerOptions));
}

string? cachedHtml = null;
DateTime lastRead = DateTime.MinValue;

string BuildHtml(IHostEnvironment host)
{
    lastRead = DateTime.Now;

    var template = File.ReadAllText(Path.Combine(host.ContentRootPath, "template.html"));

    var bookmarkerOptions = JsonSerializer.Deserialize<BookmarkerOptions>(
        File.ReadAllText(userConfigPath),
        serializerOptions);

    var pages = bookmarkerOptions?.Tabs.Select(pageOptions => new BookmarksTab
    {
        Name = pageOptions.Name,

        BookmarksFileContents = pageOptions.Files
            .Select(f => Path.IsPathRooted(f) ? f : Path.Combine(host.ContentRootPath, f))
            .Where(File.Exists)
            .Select(File.ReadAllText)
            .Select(c => JsonSerializer.Deserialize<BookmarksFileContent>(c, serializerOptions))
            .ToArray()!

    }).ToArray();

    return Render(template, pages!, userConfigPath, lastRead);
}

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

app.MapGet("/refresh", () => Results.Redirect("/"));

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
            sb.AppendLine($"<summary><h2>{bookmarkFileContent.Name}</h2></summary>");            
            sb.AppendLine($"<div>");

            foreach(var (group, groupIndex) in bookmarkFileContent.Groups.Select((v,i) => (v,i)))
            {
                var useDetailsWrapper = !string.IsNullOrEmpty(group.Name);
                
                if (useDetailsWrapper)
                {
                    sb.AppendLine($"<details open>");
                    sb.AppendLine($"<summary><h3>{group.Name}</h3></summary>");
                }

                sb.AppendLine($"<div>");
                sb.AppendLine($"<ul>");
                
                foreach(var set in group.Sets)
                {
                    sb.AppendLine("<li>");

                    sb.AppendLine( 
                        string.IsNullOrEmpty(set.Url) 
                        ? $"{set.Name}:"
                        : $"<a href=\"{set.Url}\" target=\"_blank\">{set.Name}</a>");
                                                        
                    foreach(var bookmark in set.Bookmarks)
                    {
                        sb.AppendLine($" | <a href=\"{bookmark.Url}\" target=\"_blank\">{bookmark.Name}</a>");
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


BookmarksTab WelcomeBookmarkSet()
{
    return new BookmarksTab
    {
        Name = "Welcome",
        BookmarksFileContents = new[]
        {
            new BookmarksFileContent
            {
                Name = "Welcome to Bookmarker",
                Groups = new[]
                {
                    new BookmarksGroup
                    {
                        Name = "Bookmarker",
                        Sets = new[]
                        {
                            new BookmarkSet
                            {
                                Name = "GitHub",
                                Url = "https://github.com/greek-developer/bookmarker",
                                Bookmarks = new[]
                                {
                                    new Bookmark { Name = "README",       Url = "https://github.com/greek-developer/bookmarker#readme" },
                                    new Bookmark { Name = "Issues",       Url = "https://github.com/greek-developer/bookmarker/issues" },
                                    new Bookmark { Name = "Releases",     Url = "https://github.com/greek-developer/bookmarker/releases" },
                                }
                            },
                            new BookmarkSet
                            {
                                Name = "Configuration",
                                Url = "https://github.com/greek-developer/bookmarker#configuration-format",
                            }
                        }
                    },
                    new BookmarksGroup
                    {
                        Name = "GreekDeveloper",
                        Sets = new[]
                        {
                            new BookmarkSet { Name = "Blog",  Url = "http://greekdeveloper.com" },
                        }
                    }
                }
            }
        }
    };
}