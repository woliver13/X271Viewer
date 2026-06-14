namespace X271Viewer.Domain;

public sealed class X271Node
{
    public X271Node(string label, IReadOnlyList<string> rawSegments, IReadOnlyList<X271Node> children,
        bool isCollapsedByDefault = false)
    {
        Label = label;
        RawSegments = rawSegments;
        Children = children;
        IsCollapsedByDefault = isCollapsedByDefault;
    }

    public string Label { get; }
    public IReadOnlyList<string> RawSegments { get; }
    public IReadOnlyList<X271Node> Children { get; }
    public bool IsCollapsedByDefault { get; }
    public List<string> ValidationErrors { get; } = [];
    public bool HasValidationErrors => ValidationErrors.Count > 0;
}
