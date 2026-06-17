using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X835TreeBuilderTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private static X835Document LoadFixture() =>
        new X835DocumentParser().ParseFile(FixturePath("tests835.edi"));

    // AFK-1: root has at least one GS group child
    [Fact]
    public void Build_produces_root_with_gs_group_child()
    {
        var doc  = LoadFixture();
        var root = X835TreeBuilder.Build(doc);

        Assert.Equal("835 — Remittance Advice", root.Label);
        Assert.NotEmpty(root.Children);
        Assert.Contains(root.Children, n => n.Label.Contains("GS"));
    }

    // AFK-2: claim node labels contain patient name and paid amount
    [Fact]
    public void Claim_nodes_label_contains_patient_name_and_paid_amount()
    {
        var doc  = LoadFixture();
        var root = X835TreeBuilder.Build(doc);

        var gsNode     = root.Children.First(n => n.Label.Contains("GS"));
        var claimNodes = gsNode.Children;

        Assert.NotEmpty(claimNodes);

        // Jane Doe, fully paid $100
        var janeClaim = claimNodes.FirstOrDefault(n => n.Label.Contains("JANE", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(janeClaim);
        // paid amount — $100.00 in currency format
        Assert.Contains("100.00", janeClaim.Label);

        // John Smith, partially paid $155
        var johnClaim = claimNodes.FirstOrDefault(n => n.Label.Contains("JOHN", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(johnClaim);
        Assert.Contains("155.00", johnClaim.Label);
    }

    // AFK-3: CAS adjustment node label contains the CARC plain-English description
    [Fact]
    public void Cas_adjustment_label_contains_carc_description()
    {
        var doc  = LoadFixture();
        var root = X835TreeBuilder.Build(doc);

        var gsNode = root.Children.First(n => n.Label.Contains("GS"));

        // John Smith (CLM-002) has CAS CO 45 — "Charge exceeds fee schedule/maximum allowable"
        var johnClaim = gsNode.Children.FirstOrDefault(n => n.Label.Contains("JOHN", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(johnClaim);

        var svcNode = johnClaim.Children.FirstOrDefault();
        Assert.NotNull(svcNode);

        var casNode = svcNode.Children.FirstOrDefault(n => n.Label.StartsWith("CAS"));
        Assert.NotNull(casNode);
        Assert.Contains("Charge exceeds fee schedule", casNode.Label);
    }

    // AFK-4: RARC node appears as child of the denied-with-remark claim (CLM-004 / Robert Brown, MA01)
    [Fact]
    public void Rarc_node_appears_as_child_of_denied_claim_with_moa()
    {
        var doc  = LoadFixture();
        var root = X835TreeBuilder.Build(doc);

        var gsNode = root.Children.First(n => n.Label.Contains("GS"));

        // Robert Brown is claim 4 — denied with MOA MA01
        var robertClaim = gsNode.Children.FirstOrDefault(n => n.Label.Contains("ROBERT", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(robertClaim);

        var rarcNode = robertClaim.Children.FirstOrDefault(n => n.Label.StartsWith("RARC"));
        Assert.NotNull(rarcNode);
        Assert.Contains("MA01", rarcNode.Label);
    }
}
