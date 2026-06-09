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

app.MapGet("/", (IHostEnvironment host) =>
{
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

    var html = Render(template, pages!, userConfigPath);

    return Results.Content(html, "text/html");
});

app.Run();

string Render(
    string htmlTemplate, 
    IEnumerable<BookmarksTab> bookmarkPages,
    string configurationFileLocation )
{   
    var sb = new StringBuilder();
    foreach(var (bookmarkPage, pageIndex) in bookmarkPages.Select((v,i) => (v,i)))
    {
        sb.AppendLine($"<input type=\"radio\" id=\"{pageIndex}\" name=\"tabs\" {(pageIndex == 0 ? "checked" : "")}>");
        sb.AppendLine($"<label for=\"{pageIndex}\"> {bookmarkPage.Name} </label>");
        sb.AppendLine("<div>");

        foreach(var (bookmarkFileContent, fileIndex) in bookmarkPage.BookmarksFileContents.Select((v,i) => (v,i)))
        {   

            sb.AppendLine($"<details {(fileIndex == 0 ? "open" : "")}>");
            sb.AppendLine($"<summary><h2>{bookmarkFileContent.Name}</h2></summary>");            
            sb.AppendLine($"<div>");

            foreach(var (group, groupIndex) in bookmarkFileContent.Groups.Select((v,i) => (v,i)))
            {
                var useDetailsWrapper = !string.IsNullOrEmpty(group.Name);
                
                if (useDetailsWrapper)
                {
                    sb.AppendLine($"<details {(groupIndex == 0 ? "open" : "")}>");
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
        .Replace("{{footer}}", configurationFileLocation.ToLower());
}


BookmarksTab WelcomeBookmarkSet()
{
    return new BookmarksTab
    {
        Name = "Welcome",
        BookmarksFileContents = new []
        {
            new BookmarksFileContent
            {
                Name = "These are the contents of the welcome file",
                Groups = new []
                {
                    new BookmarksGroup
                    {
                        Name = "This is a bookmark group",
                        Sets = new []
                        {
                            new BookmarkSet
                            {
                                Name = "This is a bookmark set",
                                Url = "http://localhost:5000",
                                Bookmarks = new []
                                {
                                    new Bookmark
                                    {
                                        Name = "README",
                                        Url = "http://github.com"
                                    },
                                    new Bookmark
                                    {
                                        Name = "WELCOME",
                                        Url = "http://github.com"
                                    },
                                    new Bookmark
                                    {
                                        Name = "CONTRIBUTING",
                                        Url = "http://github.com"
                                    }
                                }
                            }
                        }
                    },
                    new BookmarksGroup
                    {
                        Name = "This is another bookmark group",
                        Sets = new []
                        {
                            new BookmarkSet
                            {
                                Name = "Project 1",
                                Url = "http://localhost:5000/project1"
                            },
                            new BookmarkSet
                            {
                                Name = "Project 2",
                                Url = "http://localhost:5000/project2"
                            }
                        }
                    }
                }
            }
        }
    };
}