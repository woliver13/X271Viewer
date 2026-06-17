using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class TreeBuilderTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static X271Document LoadFull271()
    {
        var path   = Path.Combine(FixtureDir, "full271.edi");
        var parser = new X271DocumentParser();
        return parser.ParseFile(path);
    }

    private static X271Document LoadSubscriber271()
    {
        var path   = Path.Combine(FixtureDir, "subscriber271.edi");
        var parser = new X271DocumentParser();
        return parser.ParseFile(path);
    }

    // ── Cycle 1 ──────────────────────────────────────────────────────────────

    [Fact]
    public void X271Node_exposes_Label_RawSegments_Children()
    {
        var child = new X271Node("child", [], []);
        var node  = new X271Node("root", ["ISA*00*~"], [child]);

        Assert.Equal("root", node.Label);
        Assert.Equal("ISA*00*~", node.RawSegments[0]);
        Assert.Single(node.Children);
        Assert.Equal("child", node.Children[0].Label);
    }

    // ── Cycle 2 ──────────────────────────────────────────────────────────────

    [Fact]
    public void TreeBuilder_root_is_ISA_node_collapsed_by_default()
    {
        var doc  = LoadFull271();
        var root = X271TreeBuilder.Build(doc);

        Assert.StartsWith("ISA", root.Label);
        Assert.True(root.IsCollapsedByDefault);
    }

    // ── Cycle 3 ──────────────────────────────────────────────────────────────

    [Fact]
    public void TreeBuilder_ISA_has_GS_child_collapsed()
    {
        var doc  = LoadFull271();
        var root = X271TreeBuilder.Build(doc);

        Assert.Single(root.Children);
        var gs = root.Children[0];
        Assert.StartsWith("GS", gs.Label);
        Assert.True(gs.IsCollapsedByDefault);
    }

    // ── Cycle 4 ──────────────────────────────────────────────────────────────

    [Fact]
    public void TreeBuilder_GS_has_ST_child()
    {
        var doc  = LoadFull271();
        var root = X271TreeBuilder.Build(doc);
        var gs   = root.Children[0];

        Assert.Single(gs.Children);
        var st = gs.Children[0];
        Assert.StartsWith("ST", st.Label);
    }

    // ── Cycle 5 ──────────────────────────────────────────────────────────────

    [Fact]
    public void TreeBuilder_ST_has_HL_nodes_in_correct_hierarchy()
    {
        var doc  = LoadFull271();
        var root = X271TreeBuilder.Build(doc);
        var st   = root.Children[0].Children[0];

        // ST → Info Source (HL01=1, level 20)
        var infoSource = st.Children.Single(n => n.Label.Contains("Information Source"));
        Assert.Contains("1", infoSource.Label);

        // Info Source → Info Receiver (HL01=2, level 21)
        var infoReceiver = infoSource.Children.Single(n => n.Label.Contains("Information Receiver"));

        // Info Receiver → Subscriber (HL01=3, level 22)
        var subscriber = infoReceiver.Children.Single(n => n.Label.Contains("Subscriber"));

        // Subscriber → Dependent (HL01=4, level 23)
        var dependent = subscriber.Children.Single(n => n.Label.Contains("Dependent"));
        Assert.NotNull(dependent);
    }

    // ── Cycle 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public void TreeBuilder_EB_segments_grouped_by_service_type()
    {
        var doc       = LoadFull271();
        var root      = X271TreeBuilder.Build(doc);
        var st        = root.Children[0].Children[0];
        var dependent = st.Children
            .Single(n => n.Label.Contains("Information Source")).Children
            .Single(n => n.Label.Contains("Information Receiver")).Children
            .Single(n => n.Label.Contains("Subscriber")).Children
            .Single(n => n.Label.Contains("Dependent"));

        // 7 EB segments with EB01 values: 1,C,G,A,R,1,B → 6 distinct service-type groups
        var ebGroups = dependent.Children.Where(n => n.Label.StartsWith("EB")).ToList();
        Assert.Equal(6, ebGroups.Count);

        // Service type "1" group should contain 2 individual EB nodes
        var group1 = ebGroups.Single(n => n.Label.Contains(" 1"));
        Assert.Equal(2, group1.Children.Count);
    }

    // ── Cycle 7 — subscriber-as-patient (3-level hierarchy) ──────────────────

    [Fact]
    public void TreeBuilder_subscriber_as_patient_has_no_dependent_node()
    {
        var doc        = LoadSubscriber271();
        var root       = X271TreeBuilder.Build(doc);
        var st         = root.Children[0].Children[0];
        var subscriber = st.Children
            .Single(n => n.Label.Contains("Information Source")).Children
            .Single(n => n.Label.Contains("Information Receiver")).Children
            .Single(n => n.Label.Contains("Subscriber"));

        Assert.DoesNotContain(subscriber.Children, n => n.Label.Contains("Dependent"));
    }

    [Fact]
    public void TreeBuilder_subscriber_as_patient_EB_groups_appear_under_subscriber()
    {
        var doc        = LoadSubscriber271();
        var root       = X271TreeBuilder.Build(doc);
        var st         = root.Children[0].Children[0];
        var subscriber = st.Children
            .Single(n => n.Label.Contains("Information Source")).Children
            .Single(n => n.Label.Contains("Information Receiver")).Children
            .Single(n => n.Label.Contains("Subscriber"));

        // 4 EB segments: 1,C,G,B → 4 distinct service-type groups directly under subscriber
        var ebGroups = subscriber.Children.Where(n => n.Label.StartsWith("EB")).ToList();
        Assert.Equal(4, ebGroups.Count);
    }

    // ── Cycle 8 — multiple subscriber loops (P2H5) ───────────────────────────

    [Fact]
    public void TreeBuilder_multiple_subscriber_loops_all_render()
    {
        var path   = Path.Combine(FixtureDir, "multi_subscriber271.edi");
        var parser = new X271DocumentParser();
        var doc    = parser.ParseFile(path);
        var root   = X271TreeBuilder.Build(doc);
        var st     = root.Children[0].Children[0];

        var subscribers = st.Children
            .Single(n => n.Label.Contains("Information Source")).Children
            .Single(n => n.Label.Contains("Information Receiver")).Children
            .Where(n => n.Label.Contains("Subscriber"))
            .ToList();

        // Two subscriber HL loops (HL 3 and HL 4) must both appear — no truncation
        Assert.Equal(2, subscribers.Count);
        Assert.Contains(subscribers, n => n.RawSegments.Any(s => s.Contains("ABC123456789")));
        Assert.Contains(subscribers, n => n.RawSegments.Any(s => s.Contains("DEF987654321")));
    }
}
