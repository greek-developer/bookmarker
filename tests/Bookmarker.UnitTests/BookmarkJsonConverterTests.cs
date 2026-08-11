using System.Text.Json;

namespace Bookmarker.UnitTests;

// Covers specs/bookmark-files.md — the two ways a bookmark can be written, and the
// System.Text.Json gotcha that [JsonConverter] is not inherited.
public class BookmarkJsonConverterTests
{
    // The same options the app uses, so these tests exercise the real parsing tolerance.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    [Fact]
    public void Read_StringShorthand_SplitsIntoNameAndUrl()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>("\"Grafana=https://grafana.internal\"", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Grafana", bookmark.Name);
        Assert.Equal("https://grafana.internal", bookmark.Url);
    }

    [Fact]
    public void Read_StringShorthandWithEqualsInQueryString_SplitsOnFirstEqualsOnly()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>("\"Search=https://example.com/?q=1&r=2\"", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Search", bookmark.Name);
        Assert.Equal("https://example.com/?q=1&r=2", bookmark.Url);
    }

    [Fact]
    public void Read_StringShorthandWithoutEquals_YieldsNameAndEmptyUrl()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>("\"Just a label\"", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Just a label", bookmark.Name);
        Assert.Equal("", bookmark.Url);
    }

    [Fact]
    public void Read_StringShorthandWithSurroundingWhitespace_TrimsBothSides()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>("\" Logs = https://logs.example.com \"", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Logs", bookmark.Name);
        Assert.Equal("https://logs.example.com", bookmark.Url);
    }

    [Fact]
    public void Read_ObjectForm_ReadsNameAndUrl()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>(
            """{ "name": "Grafana", "url": "https://grafana.internal" }""", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Grafana", bookmark.Name);
        Assert.Equal("https://grafana.internal", bookmark.Url);
    }

    [Fact]
    public void Read_ObjectFormWithUppercaseProperties_MatchesCaseInsensitively()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>(
            """{ "Name": "Grafana", "URL": "https://grafana.internal" }""", Options);

        Assert.NotNull(bookmark);
        Assert.Equal("Grafana", bookmark.Name);
        Assert.Equal("https://grafana.internal", bookmark.Url);
    }

    [Fact]
    public void Read_ObjectFormWithBookmarks_PromotesToBookmarkSet()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>(
            """
            {
              "name": "Logs",
              "url": "https://logs.example.com",
              "bookmarks": [ "Dev=https://logs.dev.example.com", "Prod=https://logs.prod.example.com" ]
            }
            """, Options);

        var set = Assert.IsType<BookmarkSet>(bookmark);
        Assert.Equal("Logs", set.Name);
        Assert.Equal(2, set.Bookmarks.Length);
        Assert.Equal("Dev", set.Bookmarks[0].Name);
        Assert.Equal("https://logs.prod.example.com", set.Bookmarks[1].Url);
    }

    // [JsonConverter] is not inherited in System.Text.Json — BookmarkSet carries its own
    // attribute, which delegates back to BookmarkJsonConverter with the concrete type. If that
    // attribute is ever dropped, this is the test that fails.
    [Fact]
    public void Read_ShorthandTypedAsBookmarkSet_ReturnsBookmarkSet()
    {
        var set = JsonSerializer.Deserialize<BookmarkSet>("\"Grafana=https://grafana.internal\"", Options);

        Assert.IsType<BookmarkSet>(set);
        Assert.Equal("Grafana", set.Name);
        Assert.Empty(set.Bookmarks);
    }

    [Fact]
    public void Read_TrailingCommaInBookmarksArray_IsAccepted()
    {
        var bookmark = JsonSerializer.Deserialize<Bookmark>(
            """
            {
              "name": "Logs",
              "bookmarks": [ "Dev=https://dev.example.com", ],
            }
            """, Options);

        var set = Assert.IsType<BookmarkSet>(bookmark);
        Assert.Single(set.Bookmarks);
    }

    [Fact]
    public void Read_NumericToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Bookmark>("42", Options));
    }

    [Fact]
    public void Write_PlainBookmark_EmitsShorthandString()
    {
        var json = JsonSerializer.Serialize(new Bookmark { Name = "Grafana", Url = "https://grafana.internal" }, Options);

        Assert.Equal("\"Grafana=https://grafana.internal\"", json);
    }

    [Fact]
    public void Write_SetWithRelatedBookmarks_EmitsObject()
    {
        var set = new BookmarkSet
        {
            Name = "Logs",
            Url = "https://logs.example.com",
            Bookmarks = [new Bookmark { Name = "Dev", Url = "https://dev.example.com" }]
        };

        var json = JsonSerializer.Serialize(set, Options);

        Assert.Contains("\"name\": \"Logs\"", json);
        Assert.Contains("\"Dev=https://dev.example.com\"", json);
    }

    [Fact]
    public void WriteThenRead_SetWithRelatedBookmarks_RoundTrips()
    {
        var original = new BookmarkSet
        {
            Name = "Logs",
            Url = "https://logs.example.com",
            Bookmarks = [new Bookmark { Name = "Dev", Url = "https://dev.example.com" }]
        };

        var round = JsonSerializer.Deserialize<BookmarkSet>(JsonSerializer.Serialize(original, Options), Options);

        Assert.NotNull(round);
        Assert.Equal(original.Name, round.Name);
        Assert.Equal(original.Url, round.Url);
        Assert.Equal("Dev", Assert.Single(round.Bookmarks).Name);
    }
}
