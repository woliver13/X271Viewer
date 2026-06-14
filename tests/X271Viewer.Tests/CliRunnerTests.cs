using System.Text.Json;
using X271Viewer.Application;

namespace X271Viewer.Tests;

public class CliRunnerTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private static (int exit, string stdout, string stderr) Exec(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit   = CliRunner.Run(args, stdout, stderr);
        return (exit, stdout.ToString(), stderr.ToString());
    }

    // ── Cycles ───────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_valid_file_outputs_JSON_with_segments_array()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("full271.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var segments = doc.RootElement.GetProperty("segments");
        Assert.Equal(JsonValueKind.Array, segments.ValueKind);
        Assert.True(segments.GetArrayLength() > 0);
    }

    [Fact]
    public void Interpret_valid_file_outputs_ExportDocument_schema()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("full271.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("IsaRawText", out _));
        Assert.True(root.TryGetProperty("Root", out var rootNode));
        Assert.Equal(JsonValueKind.Object, rootNode.ValueKind);
        Assert.True(root.TryGetProperty("ValidationResults", out var vr));
        Assert.Equal(JsonValueKind.Array, vr.ValueKind);
    }

    [Fact]
    public void Validate_valid_file_outputs_isValid_and_errors()
    {
        var (exit, stdout, stderr) = Exec("validate", FixturePath("full271.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("isValid", out var isValid));
        Assert.Equal(JsonValueKind.False, isValid.ValueKind); // full271.edi has a missing IEA error
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.True(errors.GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData()]
    public void Help_lists_all_four_commands(params string[] args)
    {
        var (exit, stdout, _) = Exec(args);

        Assert.Equal(0, exit);
        Assert.Contains("parse",     stdout);
        Assert.Contains("interpret", stdout);
        Assert.Contains("validate",  stdout);
        Assert.Contains("view",      stdout);
    }

    [Theory]
    [InlineData("parse")]
    [InlineData("interpret")]
    [InlineData("validate")]
    public void Missing_file_writes_to_stderr_exits_1(string command)
    {
        var (exit, stdout, stderr) = Exec(command, "nonexistent_file.edi");

        Assert.Equal(1, exit);
        Assert.Empty(stdout);
        Assert.NotEmpty(stderr);
    }

    [Fact]
    public void Interpret_output_roundtrips_via_Import()
    {
        var (exit, stdout, _) = Exec("interpret", FixturePath("full271.edi"));

        Assert.Equal(0, exit);

        var exportDoc = X271JsonExporter.Import(stdout);
        Assert.NotNull(exportDoc.Root);
        Assert.False(string.IsNullOrEmpty(exportDoc.Root.Label));
    }

    [Fact]
    public void Malformed_file_writes_to_stderr_exits_2()
    {
        // not_x12.edi contains non-X12 content that triggers X271ParseException
        var (exit, stdout, stderr) = Exec("parse", FixturePath("not_x12.edi"));

        Assert.Equal(2, exit);
        Assert.Empty(stdout);
        Assert.NotEmpty(stderr);
    }
}
