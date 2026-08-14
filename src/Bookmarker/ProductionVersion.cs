
using System.Text.Json;
using System.Text.Json.Serialization;

// Build identity, written next to the app by the WriteProductionVersion* targets in
// Bookmarker.csproj. Read once at startup and surfaced in the page footer and at
// GET /api/diagnostics/version.
public class ProductionVersion
{
    public const string FileName = "ProductionVersion.json";

    public const string Unknown = "unknown";

    public string Version { get; set; } = Unknown;

    public ProductionCommit Commit { get; set; } = new();

    public ProductionBuild Build { get; set; } = new();

    // The file exactly as the build wrote it, so the diagnostics endpoint can return it verbatim.
    [JsonIgnore]
    public string RawJson { get; set; } = "";

    // The identity line shown in the page footer. The commit is shortened to its first 8
    // characters (see AGENTS.md > Production version file) — enough to identify it at a
    // glance; the full SHA stays in the JSON and at GET /api/diagnostics/version.
    public string FooterText => $"{Version} - {ShortSha} - {Build.Time}";

    private string ShortSha => Commit.Sha.Length >= 8 ? Commit.Sha[..8] : Commit.Sha;

    // Reads the file from the first directory that has one. A missing or unreadable file is not
    // an error — running from a source tree that was never published still serves the page, with
    // the identity reported as "unknown".
    public static ProductionVersion Read(IEnumerable<string> searchDirectories, JsonSerializerOptions options)
    {
        foreach (var directory in searchDirectories)
        {
            var path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) continue;

            try
            {
                var raw = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<ProductionVersion>(raw, options);
                if (parsed is null) continue;

                parsed.RawJson = raw;
                return parsed;
            }
            catch (JsonException)
            {
                // A corrupt version file must never stop the app from serving the page.
            }
        }

        return new ProductionVersion();
    }
}

public class ProductionCommit
{
    public string Message { get; set; } = ProductionVersion.Unknown;

    public string Sha { get; set; } = ProductionVersion.Unknown;
}

public class ProductionBuild
{
    public string Time { get; set; } = ProductionVersion.Unknown;
}
