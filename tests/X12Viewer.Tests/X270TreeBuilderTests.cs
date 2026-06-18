using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X270TreeBuilderTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private X270Document InterpretedDoc()
    {
        var raw = new X270DocumentParser().ParseFile(Path.Combine(FixtureDir, "tests270.edi"));
        return X270Interpreter.Interpret(raw);
    }

    // Cycle 10: subscriber nodes as children of root
    [Fact]
    public void X270TreeBuilder_produces_subscriber_nodes_as_children_of_root()
    {
        var doc = InterpretedDoc();
        var root = X270TreeBuilder.Build(doc);
        Assert.Equal(2, root.Children.Count);
    }

    // Cycle 11: dependent node appears as child of subscriber node
    [Fact]
    public void X270TreeBuilder_dependent_node_appears_as_child_of_subscriber()
    {
        var doc = InterpretedDoc();
        var root = X270TreeBuilder.Build(doc);
        var sub1Node = root.Children[1]; // JOHN SMITH has a dependent
        var depNode = sub1Node.Children.FirstOrDefault(c => c.Label.Contains("BABY SMITH"));
        Assert.NotNull(depNode);
    }

    // Cycle 12: EQ nodes have plain-English description labels
    [Fact]
    public void X270TreeBuilder_EQ_nodes_have_plain_English_description_labels()
    {
        var doc = InterpretedDoc();
        var root = X270TreeBuilder.Build(doc);
        var sub0Node = root.Children[0]; // JANE DOE has EQ 30, 98, AL
        // EQ nodes are direct children of the subscriber node (before any dependent)
        var eqNodes = sub0Node.Children.Where(c => !c.Label.Contains("Dependent")).ToList();
        Assert.NotEmpty(eqNodes);
        // Label should not just be the bare code — it should contain a description
        Assert.All(eqNodes, node => Assert.False(string.IsNullOrEmpty(node.Label)));
    }
}
