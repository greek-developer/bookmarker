using System.Text.Json;

namespace Bookmarker.UnitTests;

// Covers specs/diagnostics.md — a running build reporting which build it is.
public class ProductionVersionTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"bookmarker-tests-{Guid.NewGuid():N}");

    public ProductionVersionTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string SampleJson = """
        {
          "version": "0.8.42",
          "commit": {
            "message": "add table layout",
            "sha": "f0ba4ea1111111111111111111111111111111111"
          },
          "build": {
            "time": "2026-08-11T09:14:22Z"
          }
        }
        """;

    private string WriteVersionFile(string contents)
    {
        var path = Path.Combine(_directory, ProductionVersion.FileName);
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void Read_FileMissing_ReportsUnknownRatherThanThrowing()
    {
        var version = ProductionVersion.Read([_directory], Options);

        Assert.Equal(ProductionVersion.Unknown, version.Version);
        Assert.Equal(ProductionVersion.Unknown, version.Commit.Sha);
        Assert.Equal(ProductionVersion.Unknown, version.Build.Time);
        Assert.Equal("", version.RawJson);
    }

    [Fact]
    public void Read_ValidFile_ReadsEveryField()
    {
        WriteVersionFile(SampleJson);

        var version = ProductionVersion.Read([_directory], Options);

        Assert.Equal("0.8.42", version.Version);
        Assert.Equal("add table layout", version.Commit.Message);
        Assert.Equal("f0ba4ea1111111111111111111111111111111111", version.Commit.Sha);
        Assert.Equal("2026-08-11T09:14:22Z", version.Build.Time);
    }

    // The diagnostics endpoint returns the file verbatim, so the raw text has to survive the read.
    [Fact]
    public void Read_ValidFile_KeepsTheRawJson()
    {
        WriteVersionFile(SampleJson);

        var version = ProductionVersion.Read([_directory], Options);

        Assert.Equal(SampleJson, version.RawJson);
    }

    [Fact]
    public void Read_CorruptFile_FallsBackToUnknownInsteadOfThrowing()
    {
        WriteVersionFile("{ this is not json");

        var version = ProductionVersion.Read([_directory], Options);

        Assert.Equal(ProductionVersion.Unknown, version.Version);
    }

    [Fact]
    public void Read_SeveralDirectories_UsesTheFirstThatHasTheFile()
    {
        var second = Path.Combine(_directory, "second");
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(second, ProductionVersion.FileName), SampleJson);

        var version = ProductionVersion.Read([_directory, second], Options);

        Assert.Equal("0.8.42", version.Version);
    }

    [Fact]
    public void FooterText_FormatsAsVersionShortShaBuildTime()
    {
        WriteVersionFile(SampleJson);

        var version = ProductionVersion.Read([_directory], Options);

        // The footer shows the first 8 characters of the commit SHA.
        Assert.Equal("0.8.42 - f0ba4ea1 - 2026-08-11T09:14:22Z", version.FooterText);
    }
}
