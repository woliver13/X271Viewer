using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X271NodeFilter
{
    /// <summary>
    /// Returns a filtered view of the node list matching <paramref name="query"/>.
    /// Parent nodes are included when any descendant matches.
    /// An empty or whitespace query returns the original list unchanged.
    /// </summary>
    public static IReadOnlyList<X271Node> Filter(IReadOnlyList<X271Node> nodes, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return nodes;

        var result = new List<X271Node>();
        foreach (var node in nodes)
        {
            var pruned = Prune(node, query);
            if (pruned is not null)
                result.Add(pruned);
        }
        return result;
    }

    // Returns a (possibly rebuilt) node if it or any descendant matches, else null.
    private static X271Node? Prune(X271Node node, string query)
    {
        // Recursively prune children first
        var matchingChildren = new List<X271Node>();
        foreach (var child in node.Children)
        {
            var prunedChild = Prune(child, query);
            if (prunedChild is not null)
                matchingChildren.Add(prunedChild);
        }

        // This node matches directly
        if (SelfMatches(node, query))
        {
            // Keep all children when the node itself matches
            return node;
        }

        // At least one descendant matched — return node with only matching children
        if (matchingChildren.Count > 0)
            return new X271Node(node.Label, node.RawSegments, matchingChildren,
                node.IsCollapsedByDefault);

        return null;
    }

    private static bool SelfMatches(X271Node node, string query)
    {
        if (node.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;
        if (node.RawSegments.Any(s => s.Contains(query, StringComparison.OrdinalIgnoreCase)))
            return true;
        var interpretation = X271InterpretationEngine.Interpret(node);
        return interpretation.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
