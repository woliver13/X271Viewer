using System.Text.Json;
using woliver13.X271Viewer.Application;
using woliver13.X271Viewer.Domain;

namespace woliver13.X271Viewer.Tests;

public class JsonExportTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    // ── Cycle 1 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Export_produces_indented_JSON()
    {
        var doc = new X271ExportDocument(
            IsaRawText: "ISA*00*...",
            Root: new X271ExportNode("Root", [], "no interpretation", [], []),
            ValidationResults: []);

        var json = X271JsonExporter.Export(doc);

        Assert.Contains('\n', json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void Export_produces_valid_JSON()
    {
        var doc = new X271ExportDocument(
            IsaRawText: "ISA*00*...",
            Root: new X271ExportNode("Root", [], "no interpretation", [], []),
            ValidationResults: []);

        var json = X271JsonExporter.Export(doc);

        JsonDocument.Parse(json); // must not throw
    }

    // ── Cycle 2 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Export_roundtrip_preserves_all_fields()
    {
        var original = new X271ExportDocument(
            IsaRawText: "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *260101*1200*^*00501*000000001*0*P*:~",
            Root: new X271ExportNode(
                "Root",
                ["ISA*00*...~"],
                "root interpretation",
                ["ERR001"],
                [new X271ExportNode("Child", ["EB*1*IND~"], "child interp", [], [])]),
            ValidationResults: [new X271ExportValidationError("E001", "Missing ST segment")]);

        var json = X271JsonExporter.Export(original);
        var restored = X271JsonExporter.Import(json);

        Assert.Equal(original.IsaRawText, restored.IsaRawText);
        Assert.Equal(original.Root.Label, restored.Root.Label);
        Assert.Equal(original.Root.Interpretation, restored.Root.Interpretation);
        Assert.Single(restored.Root.ValidationErrors);
        Assert.Equal("ERR001", restored.Root.ValidationErrors[0]);
        Assert.Single(restored.Root.Children);
        Assert.Equal("Child", restored.Root.Children[0].Label);
        Assert.Single(restored.ValidationResults);
        Assert.Equal("E001", restored.ValidationResults[0].Code);
        Assert.Equal("Missing ST segment", restored.ValidationResults[0].Message);
    }

    // ── Cycle 3 ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildExportDocument_includes_validationResults()
    {
        var root = new X271Node("ISA", ["ISA*00*...~"], []);
        var validationResult = new X271ValidationResult([
            new X271ValidationError("E001", "Missing GS segment"),
            new X271ValidationError("E002", "Missing ST segment"),
        ]);

        var exportDoc = X271JsonExporter.BuildExportDocument(root, validationResult);

        Assert.Equal(2, exportDoc.ValidationResults.Count);
        Assert.Contains(exportDoc.ValidationResults, e => e.Code == "E001" && e.Message == "Missing GS segment");
        Assert.Contains(exportDoc.ValidationResults, e => e.Code == "E002" && e.Message == "Missing ST segment");
    }

    // ── Cycle 4 ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildExportDocument_includes_interpretation_text()
    {
        // EB*1*IND*30~ → "Active Coverage" should appear in interpretation
        var ebNode = new X271Node("EB 1", ["EB*1*IND*30~"], []);
        var root   = new X271Node("ISA", ["ISA*00*...~"], [ebNode]);
        var result = new X271ValidationResult([]);

        var exportDoc = X271JsonExporter.BuildExportDocument(root, result);

        var ebExport = exportDoc.Root.Children.Single();
        Assert.Contains("Active Coverage", ebExport.Interpretation);
    }

    // ── Cycle 5 ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildExportDocument_leaf_node_has_empty_children_array()
    {
        var leaf = new X271Node("EB 1", ["EB*1*IND*30~"], []);
        var root = new X271Node("ISA", ["ISA*00*...~"], [leaf]);
        var result = new X271ValidationResult([]);

        var exportDoc = X271JsonExporter.BuildExportDocument(root, result);

        var leafExport = exportDoc.Root.Children.Single();
        Assert.NotNull(leafExport.Children);
        Assert.Empty(leafExport.Children);
    }

    // ── Cycle 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildExportDocument_full271_fixture_has_required_top_level_fields()
    {
        var path    = Path.Combine(FixtureDir, "full271.edi");
        var content = File.ReadAllText(path);
        var parser  = new X271DocumentParser();
        var doc     = parser.ParseContent(content);
        var root    = X271TreeBuilder.Build(doc);
        var valResult = new X271ValidationResult([]);

        var exportDoc = X271JsonExporter.BuildExportDocument(root, valResult, doc.IsaRawText);
        var json      = X271JsonExporter.Export(exportDoc);

        using var parsed = JsonDocument.Parse(json);
        var obj = parsed.RootElement;

        Assert.True(obj.TryGetProperty("IsaRawText", out var isaText));
        Assert.False(string.IsNullOrEmpty(isaText.GetString()));

        Assert.True(obj.TryGetProperty("Root", out var rootEl));
        Assert.Equal(JsonValueKind.Object, rootEl.ValueKind);

        Assert.True(obj.TryGetProperty("ValidationResults", out var vr));
        Assert.Equal(JsonValueKind.Array, vr.ValueKind);
    }
}
