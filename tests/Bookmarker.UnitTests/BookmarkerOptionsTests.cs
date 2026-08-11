using System.Text.Json;

namespace Bookmarker.UnitTests;

// Covers specs/configuration.md — the root config a user hand-edits.
public class BookmarkerOptionsTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    [Fact]
    public void Read_RootConfig_ReadsTabsAndTheirFilesInOrder()
    {
        var options = JsonSerializer.Deserialize<BookmarkerOptions>(
            """
            {
              "Tabs": [
                { "Name": "Work", "Files": [ "C:\\proj\\.bookmarker.work.json", "C:\\proj\\.bookmarker.staging.json" ] },
                { "Name": "Personal", "Files": [ "C:\\Users\\you\\.bookmarker.personal.json" ] }
              ]
            }
            """, Options);

        Assert.NotNull(options);
        Assert.Equal(2, options.Tabs.Length);
        Assert.Equal("Work", options.Tabs[0].Name);
        Assert.Equal(2, options.Tabs[0].Files.Length);
        Assert.Equal(@"C:\proj\.bookmarker.work.json", options.Tabs[0].Files[0]);
        Assert.Equal("Personal", options.Tabs[1].Name);
    }

    [Fact]
    public void Read_LowercaseProperties_MatchesCaseInsensitively()
    {
        var options = JsonSerializer.Deserialize<BookmarkerOptions>(
            """{ "tabs": [ { "name": "Work", "files": [ "a.json" ] } ] }""", Options);

        Assert.NotNull(options);
        Assert.Equal("Work", Assert.Single(options.Tabs).Name);
    }

    [Fact]
    public void Read_TrailingCommas_AreAccepted()
    {
        var options = JsonSerializer.Deserialize<BookmarkerOptions>(
            """
            {
              "Tabs": [
                { "Name": "Work", "Files": [ "a.json", ], },
              ],
            }
            """, Options);

        Assert.NotNull(options);
        Assert.Single(options.Tabs);
    }

    [Fact]
    public void Read_ConfigWithoutTabs_YieldsEmptyTabsRatherThanNull()
    {
        var options = JsonSerializer.Deserialize<BookmarkerOptions>("{}", Options);

        Assert.NotNull(options);
        Assert.Empty(options.Tabs);
    }

    [Fact]
    public void Read_ContentFileWithGroupsAndSets_BuildsTheFullHierarchy()
    {
        var content = JsonSerializer.Deserialize<BookmarksFileContent>(
            """
            {
              "name": "Infrastructure",
              "groups": [
                {
                  "name": "Monitoring",
                  "layout": "table",
                  "sets": [
                    { "name": "Logs", "url": "https://logs.example.com", "bookmarks": [ "Dev=https://dev.example.com" ] },
                    "Dashboard=https://dashboard.example.com"
                  ]
                }
              ]
            }
            """, Options);

        Assert.NotNull(content);
        Assert.Equal("Infrastructure", content.Name);

        var group = Assert.Single(content.Groups);
        Assert.Equal("Monitoring", group.Name);
        Assert.Equal("table", group.Layout);
        Assert.Equal(2, group.Sets.Length);

        Assert.Equal("Logs", group.Sets[0].Name);
        Assert.Equal("Dev", Assert.Single(group.Sets[0].Bookmarks).Name);

        Assert.Equal("Dashboard", group.Sets[1].Name);
        Assert.Empty(group.Sets[1].Bookmarks);
    }

    [Fact]
    public void Read_GroupWithoutLayout_DefaultsToListRendering()
    {
        var content = JsonSerializer.Deserialize<BookmarksFileContent>(
            """{ "name": "S", "groups": [ { "name": "G", "sets": [ "A=https://a" ] } ] }""", Options);

        Assert.NotNull(content);
        Assert.Equal("", Assert.Single(content.Groups).Layout);
    }
}
