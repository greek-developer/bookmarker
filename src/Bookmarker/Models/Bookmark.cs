
using System.Text.Json;
using System.Text.Json.Serialization;

public class BookmarksTab
{
    public string Name {get; set; } = "";
    public BookmarksFileContent[] BookmarksFileContents { get; set; } = Array.Empty<BookmarksFileContent>();
}


public class BookmarksFileContent
{
    // The name of the file
    public string Name { get; set; } = "";
    
    // List of top-level groups or bookmarks
    public BookmarksGroup[] Groups { get; set; } = Array.Empty<BookmarksGroup>();
}

public class BookmarksGroup
{
    // The name of the group
    public string Name { get; set; } = "";

    // List of bookmarks or subgroups
    public BookmarkSet[] Sets { get; set; } = Array.Empty<BookmarkSet>();

    // Render mode: "" / "list" (default) renders an indented link list;
    // "table" renders the sets as a column-aligned grid (one row per set,
    // one column per related bookmark). Compared case-insensitively.
    public string Layout { get; set; } = "";
}

[JsonConverter(typeof(BookmarkSetJsonConverter))]
public class BookmarkSet: Bookmark
{
    // List of additional urls to be displayed on the same line or below based on design
    public Bookmark[] Bookmarks { get; set; } = Array.Empty<Bookmark>();
}

[JsonConverter(typeof(BookmarkJsonConverter))]
public class Bookmark
{
    // The name of the bookmark
    public string Name { get; set; } = "";
    
    // The Main url for the bookmark
    public string Url { get; set; } = ""; 

}

public class BookmarkJsonConverter : JsonConverter<Bookmark>
{
    public override Bookmark Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString() ?? string.Empty;
            var separatorIndex = raw.IndexOf('=');

            var name = separatorIndex < 0 ? raw : raw[..separatorIndex].Trim();
            var url  = separatorIndex < 0 ? string.Empty : raw[(separatorIndex + 1)..].Trim();

            return typeToConvert == typeof(BookmarkSet)
                ? new BookmarkSet { Name = name, Url = url }
                : new Bookmark   { Name = name, Url = url };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var jsonObject = JsonDocument.ParseValue(ref reader);
            var root = jsonObject.RootElement;

            var name = GetPropertyValue(root, "name");
            var url  = GetPropertyValue(root, "url");

            // find bookmarks array case-insensitively
            Bookmark[]? bookmarks = null;
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "bookmarks", StringComparison.OrdinalIgnoreCase))
                {
                    bookmarks = JsonSerializer.Deserialize<Bookmark[]>(prop.Value.GetRawText(), options);
                    break;
                }
            }

            if (typeToConvert == typeof(BookmarkSet) || bookmarks is { Length: > 0 })
                return new BookmarkSet { Name = name, Url = url, Bookmarks = bookmarks ?? [] };

            return new Bookmark { Name = name, Url = url };
        }

        throw new JsonException("Bookmark must be either a string in 'name=url' format or an object.");
    }

    public override void Write(Utf8JsonWriter writer, Bookmark value, JsonSerializerOptions options)
    {
        if (value is BookmarkSet set && set.Bookmarks.Length > 0)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            writer.WriteString("url", value.Url);
            writer.WritePropertyName("bookmarks");
            JsonSerializer.Serialize(writer, set.Bookmarks, options);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue($"{value.Name}={value.Url}");
        }
    }

    private static string GetPropertyValue(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : string.Empty;
            }
        }

        return string.Empty;
    }
}

public class BookmarkSetJsonConverter : JsonConverter<BookmarkSet>
{
    private static readonly BookmarkJsonConverter _inner = new();

    public override BookmarkSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => (BookmarkSet)_inner.Read(ref reader, typeof(BookmarkSet), options)!;

    public override void Write(Utf8JsonWriter writer, BookmarkSet value, JsonSerializerOptions options)
        => _inner.Write(writer, value, options);
}
