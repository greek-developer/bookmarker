using System.Text.Json;

public static class Defaults
{
    public static void EnsureUserConfig(string userConfigPath, JsonSerializerOptions serializerOptions)
    {
        if (File.Exists(userConfigPath)) return;

        Console.WriteLine($"No configuration file found at {userConfigPath}, creating defaults.");

        var configDir = Path.GetDirectoryName(userConfigPath)!;
        Directory.CreateDirectory(configDir);
        var welcomeFilePath = Path.Combine(configDir, ".bookmarker.welcome.json");
        var socialFilePath  = Path.Combine(configDir, ".bookmarker.social.json");
        var googleFilePath  = Path.Combine(configDir, ".bookmarker.google.json");

        var defaultOptions = new BookmarkerOptions
        {
            Tabs = new[]
            {
                new BookmarkerTabsOptions
                {
                    Name  = "Welcome",
                    Files = new[] { welcomeFilePath, socialFilePath }
                },
                new BookmarkerTabsOptions
                {
                    Name  = "Google",
                    Files = new[] { googleFilePath }
                },
            }
        };

        File.WriteAllText(userConfigPath,  JsonSerializer.Serialize(defaultOptions,    serializerOptions));
        File.WriteAllText(welcomeFilePath, JsonSerializer.Serialize(WelcomeContent(),  serializerOptions));
        File.WriteAllText(socialFilePath,  JsonSerializer.Serialize(SocialContent(),   serializerOptions));
        File.WriteAllText(googleFilePath,  JsonSerializer.Serialize(GoogleContent(),   serializerOptions));
    }

    private static BookmarksFileContent WelcomeContent() => new()
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
                        Url  = "https://github.com/greek-developer/bookmarker",
                        Bookmarks = new[]
                        {
                            new Bookmark { Name = "README",   Url = "https://github.com/greek-developer/bookmarker#readme" },
                            new Bookmark { Name = "Issues",   Url = "https://github.com/greek-developer/bookmarker/issues" },
                            new Bookmark { Name = "Releases", Url = "https://github.com/greek-developer/bookmarker/releases" },
                        }
                    },
                    new BookmarkSet
                    {
                        Name = "Configuration",
                        Url  = "https://github.com/greek-developer/bookmarker#configuration-format",
                    }
                }
            },
            new BookmarksGroup
            {
                Name = "GreekDeveloper",
                Sets = new[]
                {
                    new BookmarkSet { Name = "Blog", Url = "http://greekdeveloper.com" },
                }
            }
        }
    };

    private static BookmarksFileContent SocialContent() => new()
    {
        Name = "Social",
        Groups = new[]
        {
            new BookmarksGroup
            {
                Name = "Social Media",
                Sets = new[]
                {
                    new BookmarkSet { Name = "Facebook", Url = "https://facebook.com" },
                    new BookmarkSet { Name = "X",        Url = "https://x.com" },
                    new BookmarkSet { Name = "YouTube",  Url = "https://youtube.com" },
                }
            }
        }
    };

    private static BookmarksFileContent GoogleContent() => new()
    {
        Name = "Google",
        Groups = new[]
        {
            new BookmarksGroup
            {
                Name = "Google",
                Sets = new[]
                {
                    new BookmarkSet { Name = "Gmail",    Url = "https://mail.google.com" },
                    new BookmarkSet { Name = "Calendar", Url = "https://calendar.google.com" },
                    new BookmarkSet { Name = "Gemini",   Url = "https://gemini.google.com" },
                }
            }
        }
    };
}
