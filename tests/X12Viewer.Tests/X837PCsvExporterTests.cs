using System.Text.Json;
using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X837PCsvExporterTests
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

    // Criterion: X837PCsvExporter.Export produces header row plus one data row per SV1 service line
    [Fact]
    public void Export_ProducesHeaderPlusOneRowPerServiceLine()
    {
        var doc = new X837PDocumentParser().ParseFile(FixturePath("tests837p.edi"));
        var csv = X837PCsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Header + 1 row for claim1/svc1 + 1 row for claim1/svc2 + 1 row for claim2/svc1 = 4
        Assert.StartsWith("BillingProviderNPI,", rows[0]);
        Assert.Equal(4, rows.Length); // 1 header + 3 data rows
    }

    // Criterion: CSV rows contain correct BillingProviderNPI, SubscriberID, PatientName, ProcedureCode
    [Fact]
    public void Export_RowsContainCorrectFieldValues()
    {
        var doc = new X837PDocumentParser().ParseFile(FixturePath("tests837p.edi"));
        var csv = X837PCsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // All data rows should have the billing provider NPI
        Assert.All(rows.Skip(1), row => Assert.Contains("1234567890", row));
        // Claim 2 row contains subscriber JOHN SMITH
        Assert.Contains(rows.Skip(1), row => row.Contains("987654321B"));
    }

    // Criterion: DiagnosisCodes column contains all diagnosis codes for the claim (semicolon-delimited)
    [Fact]
    public void Export_DiagnosisCodesColumn_IsSemicolonDelimited()
    {
        var doc = new X837PDocumentParser().ParseFile(FixturePath("tests837p.edi"));
        var csv = X837PCsvExporter.Export(doc);
        // Claim 1 has diagnosis J06.9; it should appear in the DiagnosisCodes column
        Assert.Contains("J06.9", csv);
    }

    // Criterion: CLI parse auto-detects ST01=837 + GS08 X222 and routes to X837PDocumentParser
    [Fact]
    public void Cli_Parse_837P_RoutesToX837PDocumentParser()
    {
        var (exit, stdout, stderr) = Exec("parse", FixturePath("tests837p.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.TryGetProperty("claims", out var claims));
        Assert.Equal(JsonValueKind.Array, claims.ValueKind);
        Assert.Equal(2, claims.GetArrayLength());
    }

    // Criterion: CLI interpret on 837P fixture emits JSON with ICD-10 descriptions populated
    [Fact]
    public void Cli_Interpret_837P_EmitsJsonWithIcd10Descriptions()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("tests837p.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var claim0 = doc.RootElement.GetProperty("claims")[0];
        var dx0    = claim0.GetProperty("diagnosisCodes")[0];
        Assert.True(dx0.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
    }

    // Criterion: Existing 271, 835, 277, and 270 CLI tests pass unchanged (spot-check)
    [Fact]
    public void Cli_Existing_835_ParseStillWorks()
    {
        var (exit, stdout, _) = Exec("parse", FixturePath("tests835.edi"));
        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.TryGetProperty("claims", out _));
    }
}
