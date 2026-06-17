using System.Text.Json;
using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X835InterpreterTests
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

    // AFK-1: CAS02 code 45 resolves correctly
    [Fact]
    public void CAS02_code_45_resolves_to_charge_exceeds_fee_schedule()
    {
        var desc = X12CodeTable.Resolve("CAS02", "45");
        Assert.Equal("Charge exceeds fee schedule/maximum allowable", desc);
    }

    // AFK-2: CAS02 code 4 resolves correctly
    [Fact]
    public void CAS02_code_4_resolves_to_not_covered_by_plan()
    {
        var desc = X12CodeTable.Resolve("CAS02", "4");
        Assert.Equal("The service/equipment/drug is not covered by the plan", desc);
    }

    // AFK-3: MOA09 code MA01 resolves to non-empty string
    [Fact]
    public void MOA09_code_MA01_resolves_to_non_empty_string()
    {
        var desc = X12CodeTable.Resolve("MOA09", "MA01");
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        Assert.DoesNotContain("unrecognized", desc);
    }

    // AFK-4: Unknown CARC returns fallback string
    [Fact]
    public void CAS02_unknown_code_returns_unrecognized_fallback()
    {
        var desc = X12CodeTable.Resolve("CAS02", "9999");
        Assert.Equal("9999 (unrecognized code)", desc);
    }

    // AFK-5: X835Interpreter enriches adjustments with non-empty ReasonDescription
    [Fact]
    public void X835Interpreter_enriches_adjustments_with_reason_descriptions()
    {
        var doc = new X835DocumentParser().ParseFile(FixturePath("tests835.edi"));
        var enriched = X835Interpreter.Interpret(doc);

        // All service lines that have adjustments should have non-empty descriptions
        var claimsWithAdjs = enriched.Claims
            .SelectMany(c => c.ServiceLines)
            .SelectMany(l => l.Adjustments)
            .ToList();

        Assert.NotEmpty(claimsWithAdjs);
        foreach (var adj in claimsWithAdjs)
        {
            Assert.NotNull(adj.ReasonDescription);
            Assert.NotEmpty(adj.ReasonDescription);
        }
    }

    // AFK-6: CLI interpret on 835 emits JSON with reasonDescription present and non-empty
    [Fact]
    public void Cli_interpret_835_emits_json_with_reasonDescription_present_and_non_empty()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("tests835.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        var claims = doc.RootElement.GetProperty("claims");
        Assert.Equal(JsonValueKind.Array, claims.ValueKind);

        // Find a claim with adjustments (CLM-002 has CAS CO 45 45.00)
        bool foundReasonDescription = false;
        foreach (var claim in claims.EnumerateArray())
        {
            if (!claim.TryGetProperty("serviceLines", out var lines)) continue;
            foreach (var line in lines.EnumerateArray())
            {
                if (!line.TryGetProperty("adjustments", out var adjs)) continue;
                foreach (var adj in adjs.EnumerateArray())
                {
                    if (adj.TryGetProperty("reasonDescription", out var rd) &&
                        rd.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrEmpty(rd.GetString()))
                    {
                        foundReasonDescription = true;
                    }
                }
            }
        }
        Assert.True(foundReasonDescription, "Expected at least one adjustment with a non-empty reasonDescription in the JSON output");
    }

    // AFK-7: CLI interpret on 271 still works
    [Fact]
    public void Cli_interpret_271_still_works()
    {
        var (exit, stdout, stderr) = Exec("interpret", FixturePath("full271.edi"));

        Assert.Equal(0, exit);
        Assert.Empty(stderr);

        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.TryGetProperty("IsaRawText", out _));
        Assert.True(doc.RootElement.TryGetProperty("Root", out _));
    }
}
