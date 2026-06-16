using X271Viewer.Application;
using X271Viewer.Domain;

namespace X271Viewer.Tests;

public class NodeFilterTests
{
    // helper
    private static X271Node Leaf(string label, params string[] rawSegs) =>
        new(label, rawSegs, []);

    // ── Cycle 1 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EB",  "EB — Service Type 30")]
    [InlineData("eb",  "EB — Service Type 30")]
    [InlineData("service", "EB — Service Type 30")]
    [InlineData("SERVICE", "EB — Service Type 30")]
    public void Filter_matches_node_label_containing_query(string query, string label)
    {
        var node = new X271Node(label, [], []);
        var results = X271NodeFilter.Filter([node], query);
        Assert.Single(results);
        Assert.Equal(label, results[0].Label);
    }

    // ── Cycle 2 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EB",          "EB*1*IND*30*PR~")]
    [InlineData("IND",         "EB*1*IND*30*PR~")]
    [InlineData("ind",         "EB*1*IND*30*PR~")]
    [InlineData("XYZ987654321","NM1*IL*1*DOE*JANE***MI*XYZ987654321~")]
    public void Filter_matches_raw_segment_containing_query(string query, string rawSeg)
    {
        var node = Leaf("Some Node", rawSeg);
        var results = X271NodeFilter.Filter([node], query);
        Assert.Single(results);
    }

    // ── Cycle 3 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Active Coverage")]   // EB01 "1" → "Active Coverage"
    [InlineData("active coverage")]
    [InlineData("Individual")]        // EB02 "IND" → "Individual"
    [InlineData("Health Benefit")]    // EB03 "30" → "Health Benefit Plan Coverage"
    public void Filter_matches_interpretation_label_containing_query(string query)
    {
        // EB*1*IND*30*PR~ → interpretation contains "Active Coverage", "Individual", "Health Benefit Plan Coverage"
        var node = Leaf("EB 1/IND/30", "EB*1*IND*30*PR~");
        var results = X271NodeFilter.Filter([node], query);
        Assert.Single(results);
    }

    // ── Cycle 4 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Filter_returns_empty_when_no_nodes_match()
    {
        var nodes = new[]
        {
            Leaf("EB — Service Type 30", "EB*1*IND*30*PR~"),
            Leaf("HL — Subscriber (3)",  "HL*3*2*22*0~"),
        };
        var results = X271NodeFilter.Filter(nodes, "XYZZY_NOMATCH");
        Assert.Empty(results);
    }

    // ── Cycle 5 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Filter_returns_full_tree_when_query_is_empty_or_whitespace(string query)
    {
        var nodes = new[]
        {
            Leaf("EB — Service Type 30", "EB*1*IND*30*PR~"),
            Leaf("HL — Subscriber (3)",  "HL*3*2*22*0~"),
        };
        var results = X271NodeFilter.Filter(nodes, query);
        Assert.Equal(2, results.Count);
    }

    // ── Cycle 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Filter_includes_parent_node_when_child_matches()
    {
        // Parent label "ISA" does NOT contain "Active Coverage".
        // Child "EB 1" has raw "EB*1*IND*30*PR~" whose interpretation includes "Active Coverage".
        var child  = Leaf("EB 1/IND/30", "EB*1*IND*30*PR~");
        var parent = new X271Node("ISA — Interchange", [], [child]);

        var results = X271NodeFilter.Filter([parent], "Active Coverage");

        Assert.Single(results);                        // parent is included
        Assert.Equal("ISA — Interchange", results[0].Label);
        Assert.Single(results[0].Children);            // child is retained
    }

    [Fact]
    public void Filter_excludes_non_matching_sibling_children()
    {
        var matchingChild    = Leaf("EB 1/IND/30", "EB*1*IND*30*PR~");
        var nonMatchingChild = Leaf("HL — Subscriber", "HL*3*2*22*0~");
        var parent = new X271Node("ISA — Interchange", [], [matchingChild, nonMatchingChild]);

        var results = X271NodeFilter.Filter([parent], "Active Coverage");

        Assert.Single(results);
        Assert.Single(results[0].Children);            // only the matching child
        Assert.Equal("EB 1/IND/30", results[0].Children[0].Label);
    }
}
