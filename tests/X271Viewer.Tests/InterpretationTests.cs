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

    // ── Cycle 1 (Issue 15) ───────────────────────────────────────────────────

    [Theory]
    [InlineData("VS", "Visits")]
    [InlineData("DY", "Days")]
    [InlineData("MN", "Minutes")]
    [InlineData("HH", "Hours")]
    [InlineData("WK", "Weeks")]
    [InlineData("MO", "Months")]
    [InlineData("YR", "Years")]
    [InlineData("UN", "Units")]
    public void X12CodeTable_HSD01_quantity_qualifier_resolves_to_label(string code, string expected)
    {
        Assert.Equal(expected, X12CodeTable.Resolve("HSD01", code));
    }

    // ── Cycle 2 (Issue 15) ───────────────────────────────────────────────────

    [Theory]
    [InlineData("22", "Calendar Year")]
    [InlineData("23", "Plan Year")]
    [InlineData("27", "Unlimited")]
    public void X12CodeTable_HSD05_reuses_EB06_time_period_table(string code, string expected)
    {
        // HSD05 uses the same codes as EB06 — resolve via EB06 table
        Assert.Equal(expected, X12CodeTable.Resolve("EB06", code));
    }

    // ── Cycles 3-5 (Issue 15) — TreeBuilder companion segments ───────────────

    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static X271Node LoadEbCompanionSubscriberNode()
    {
        var path      = Path.Combine(FixtureDir, "eb_with_companions.edi");
        var parser    = new X271DocumentParser();
        var doc       = parser.ParseFile(path);
        var root      = X271TreeBuilder.Build(doc);
        // ISA → GS → ST → InfoSource → InfoReceiver → Subscriber
        return root.Children[0].Children[0].Children
            .Single(n => n.Label.Contains("Information Source")).Children
            .Single(n => n.Label.Contains("Information Receiver")).Children
            .Single(n => n.Label.Contains("Subscriber"));
    }

    [Fact]
    public void TreeBuilder_EB_leaf_node_carries_companion_HSD_and_MSG_segments()
    {
        var subscriber = LoadEbCompanionSubscriberNode();
        // EB "1" group → first leaf → should carry HSD + MSG + REF + DTP
        var ebGroup = subscriber.Children.First(n => n.Label.StartsWith("EB"));
        var leaf    = ebGroup.Children.First();

        Assert.Contains(leaf.RawSegments, s => s.StartsWith("HSD"));
        Assert.Contains(leaf.RawSegments, s => s.StartsWith("MSG"));
        Assert.Contains(leaf.RawSegments, s => s.StartsWith("REF"));
        Assert.Contains(leaf.RawSegments, s => s.StartsWith("DTP"));
    }

    [Fact]
    public void TreeBuilder_EB_leaf_node_with_no_companions_is_unaffected()
    {
        var subscriber = LoadEbCompanionSubscriberNode();
        // EB "B" group has no companions — leaf should only have the EB segment
        var ebGroupB = subscriber.Children.Single(n => n.Label.Contains("EB — Service Type B"));
        var leaf     = ebGroupB.Children.Single();

        Assert.Single(leaf.RawSegments);
        Assert.StartsWith("EB", leaf.RawSegments[0]);
    }

    [Fact]
    public void TreeBuilder_eb_with_companions_fixture_loads_correctly()
    {
        var subscriber = LoadEbCompanionSubscriberNode();
        var ebGroups   = subscriber.Children.Where(n => n.Label.StartsWith("EB")).ToList();

        // 3 EB segments: EB01=1, EB01=C, EB01=B → 3 distinct groups
        Assert.Equal(3, ebGroups.Count);
    }

    // ── Cycles 6-9 (Issue 15) — Engine companion segment formatting ───────────

    [Fact]
    public void InterpretationEngine_formats_HSD_as_delivery_detail()
    {
        // HSD*VS*30***22** → "Delivery: 30 Visits per Calendar Year"
        var node = new X271Node("EB 1/IND",
            ["EB*1*IND*30*PR~", "HSD*VS*30***22**~"], []);

        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Delivery:", result);
        Assert.Contains("30", result);
        Assert.Contains("Visits", result);
        Assert.Contains("Calendar Year", result);
    }

    [Fact]
    public void InterpretationEngine_formats_MSG_as_note()
    {
        var node = new X271Node("EB 1/IND",
            ["EB*1*IND*30*PR~", "MSG*Up to 30 outpatient visits.~"], []);

        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Note:", result);
        Assert.Contains("Up to 30 outpatient visits.", result);
    }

    [Fact]
    public void InterpretationEngine_formats_DTP_as_date_period()
    {
        // DTP*291*RD8*20260101-20261231~
        var node = new X271Node("EB 1/IND",
            ["EB*1*IND*30*PR~", "DTP*291*RD8*20260101-20261231~"], []);

        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Date/Period:", result);
        Assert.Contains("20260101-20261231", result);
    }

    [Fact]
    public void InterpretationEngine_formats_REF_as_reference()
    {
        // REF*18*GRP-12345~
        var node = new X271Node("EB 1/IND",
            ["EB*1*IND*30*PR~", "REF*18*GRP-12345~"], []);

        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Reference:", result);
        Assert.Contains("GRP-12345", result);
    }

    // ── Cycle 10 (Issue 15) — Regression ─────────────────────────────────────

    [Fact]
    public void InterpretationEngine_EB_node_without_companions_still_produces_correct_output()
    {
        var node = new X271Node("EB C/IND", ["EB*C*IND*30*MC*2000~"], []);
        var result = X271InterpretationEngine.Interpret(node);

        Assert.Contains("Deductible", result);
        Assert.Contains("$2,000.00", result);
        Assert.DoesNotContain("Delivery:", result);
        Assert.DoesNotContain("Note:", result);
    }
}
