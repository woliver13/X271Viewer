using System.Text.Json;
using woliver13.X12Viewer.Application;

namespace woliver13.X12Viewer.Tests;

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

    // ── Human-readable stderr (P6H2) ─────────────────────────────────────────

    [Theory]
    [InlineData("parse")]
    [InlineData("interpret")]
    [InlineData("validate")]
    public void Error_message_on_missing_file_is_human_readable(string command)
    {
        var (_, _, stderr) = Exec(command, "nonexistent_file.edi");

        Assert.StartsWith("Error:", stderr.TrimStart());
        Assert.DoesNotContain(" at ", stderr);
        Assert.DoesNotContain("Exception", stderr);
        Assert.Contains("nonexistent_file.edi", stderr);
    }

    [Fact]
    public void Error_message_on_parse_failure_is_human_readable()
    {
        var (_, _, stderr) = Exec("parse", FixturePath("not_x12.edi"));

        Assert.StartsWith("Error:", stderr.TrimStart());
        Assert.DoesNotContain(" at ", stderr);
        Assert.DoesNotContain("Exception", stderr);
    }

    // ── Phase 2: 835 routing ─────────────────────────────────────────────────

    [Fact]
    public void Cli_parse_835_routes_to_X835_parser_exits_0_JSON()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("tests835.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("claims", out var claims));
        Assert.Equal(JsonValueKind.Array, claims.ValueKind);
        Assert.Equal(4, claims.GetArrayLength());
    }

    [Fact]
    public void Cli_parse_271_still_works_unchanged()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("full271.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.TryGetProperty("segments", out var segs));
        Assert.Equal(JsonValueKind.Array, segs.ValueKind);
    }

    [Fact]
    public void Cli_interpret_835_emits_X835Document_as_JSON()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("tests835.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.TryGetProperty("claims", out _));
    }

    // ── Phase 6: 835 validation CLI ──────────────────────────────────────────

    [Fact]
    public void Cli_validate_835_exits_0()
    {
        var (exit, stdout, stderr) = Exec("validate", FixturePath("tests835.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);
        using var doc = JsonDocument.Parse(stdout);
        Assert.Equal(JsonValueKind.True, doc.RootElement.GetProperty("isValid").ValueKind);
    }

    [Fact]
    public void Cli_validate_malformed_835_exits_nonzero_with_human_readable_error()
    {
        var (exit, _, stderr) = Exec("validate", FixturePath("malformed835.edi"));

        Assert.NotEqual(0, exit);
        Assert.StartsWith("Error:", stderr.TrimStart());
    }

    // ── Phase 7: 277 routing ─────────────────────────────────────────────────

    [Fact]
    public void Cli_parse_277_routes_to_X277_parser_exits_0_JSON()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("tests277.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);
        Assert.Contains("claimId", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cli_interpret_277_emits_enriched_JSON_with_StatusDescription()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("tests277.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);
        Assert.Contains("statusDescription", stdout, StringComparison.OrdinalIgnoreCase);
    }

    // ── Phase 8: 270 routing ─────────────────────────────────────────────────

    [Fact]
    public void CliRunner_parse_routes_270_to_X270DocumentParser()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("tests270.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("subscribers", out var subs));
        Assert.Equal(JsonValueKind.Array, subs.ValueKind);
        Assert.Equal(2, subs.GetArrayLength());
    }

    [Fact]
    public void CliRunner_interpret_270_emits_plain_English_service_type_descriptions()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("tests270.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var sub0 = doc.RootElement.GetProperty("subscribers")[0];
        var eq0 = sub0.GetProperty("serviceTypeQueries")[0];
        Assert.True(eq0.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
    }
}
