using X271Viewer.Application;
using X271Viewer.Domain;

namespace X271Viewer.Tests;

public class InterpretationTests
{
    // ── Cycle 1 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_resolves_known_EB01_code_to_label()
    {
        var label = X12CodeTable.Resolve("EB01", "1");
        Assert.Equal("Active Coverage", label);
    }

    // ── Cycle 2 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_resolves_known_EB02_coverage_level_code()
    {
        var label = X12CodeTable.Resolve("EB02", "IND");
        Assert.Equal("Individual", label);
    }

    // ── Cycle 3 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_resolves_known_EB03_service_type_code()
    {
        var label = X12CodeTable.Resolve("EB03", "30");
        Assert.Equal("Health Benefit Plan Coverage", label);
    }

    // ── Cycle 4 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EB01", "ZZZ", "ZZZ (unrecognized code)")]
    [InlineData("EB02", "XYZ", "XYZ (unrecognized code)")]
    [InlineData("UNKNOWN_TABLE", "1", "1 (unrecognized code)")]
    public void X12CodeTable_returns_raw_code_with_indicator_for_unknown_input(
        string table, string code, string expected)
    {
        var result = X12CodeTable.Resolve(table, code);
        Assert.Equal(expected, result);
    }

    // ── Cycle 5 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_all_EB01_codes_resolve_to_non_empty_labels()
    {
        var table = X12CodeTable.GetTable("EB01");
        Assert.NotEmpty(table);
        foreach (var (code, label) in table)
        {
            Assert.False(string.IsNullOrWhiteSpace(label),
                $"EB01 code '{code}' has empty label");
            Assert.DoesNotContain("unrecognized", label,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── Cycle 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_all_EB02_codes_resolve_to_non_empty_labels()
    {
        var table = X12CodeTable.GetTable("EB02");
        Assert.NotEmpty(table);
        foreach (var (code, label) in table)
        {
            Assert.False(string.IsNullOrWhiteSpace(label),
                $"EB02 code '{code}' has empty label");
            Assert.DoesNotContain("unrecognized", label,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── Cycle 7 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X12CodeTable_all_EB03_service_type_codes_resolve_to_non_empty_labels()
    {
        var table = X12CodeTable.GetTable("EB03");
        Assert.NotEmpty(table);
        foreach (var (code, label) in table)
        {
            Assert.False(string.IsNullOrWhiteSpace(label),
                $"EB03 code '{code}' has empty label");
            Assert.DoesNotContain("unrecognized", label,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── Cycle 8 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2000",  "$2,000.00")]
    [InlineData("450",   "$450.00")]
    [InlineData("1500",  "$1,500.00")]
    [InlineData("10.50", "$10.50")]
    [InlineData("0",     "$0.00")]
    public void InterpretationEngine_formats_dollar_amounts_correctly(
        string raw, string expected)
    {
        var result = X271InterpretationEngine.FormatAmount(raw);
        Assert.Equal(expected, result);
    }

    // ── Cycle 9 ──────────────────────────────────────────────────────────────

    [Fact]
    public void InterpretationEngine_interprets_EB_deductible_node_with_amount()
    {
        // EB*G*IND*30*MC*450~ → Out-of-Pocket (Stop Loss), Individual, Health Benefit Plan Coverage, $450.00
        var node = new X271Node("EB G", ["EB*G*IND*30*MC*450~"], []);
        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Out-of-Pocket (Stop Loss)", result);
        Assert.Contains("Individual", result);
        Assert.Contains("$450.00", result);
    }

    [Fact]
    public void InterpretationEngine_interprets_EB_copayment_node()
    {
        // EB*C*IND*30*MC*2000~ → Deductible, Individual, Health Benefit Plan Coverage, $2,000.00
        var node = new X271Node("EB C", ["EB*C*IND*30*MC*2000~"], []);
        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Deductible", result);
        Assert.Contains("Individual", result);
        Assert.Contains("$2,000.00", result);
    }

    // ── Cycle 10 ─────────────────────────────────────────────────────────────

    [Fact]
    public void InterpretationEngine_interprets_NM1_subscriber_with_DMG_segments()
    {
        // NM1*IL*1*DOE*JANE*M***MI*XYZ987654321~ + DMG*D8*19850315*F~
        var node = new X271Node("Subscriber — HL 3",
            [
                "NM1*IL*1*DOE*JANE*M***MI*XYZ987654321~",
                "DMG*D8*19850315*F~"
            ], []);

        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Subscriber", result);
        Assert.Contains("JANE", result);
        Assert.Contains("DOE", result);
        Assert.Contains("XYZ987654321", result);
        Assert.Contains("March 15, 1985", result);
        Assert.Contains("Female", result);
    }

    // ── Cycle 11 ─────────────────────────────────────────────────────────────

    [Fact]
    public void InterpretationEngine_degrades_gracefully_on_EB_with_unrecognized_codes()
    {
        // ZZ is not a valid EB01; ZZTYPE is not a valid EB03
        var node = new X271Node("EB ZZ", ["EB*ZZ*IND*ZZTYPE**999~"], []);

        var result = X271InterpretationEngine.Interpret(node);

        // Must not throw; must contain raw codes with indicator
        Assert.Contains("ZZ (unrecognized code)", result);
        Assert.Contains("ZZTYPE (unrecognized code)", result);
        Assert.DoesNotContain("Exception", result);
    }
}
