namespace woliver13.X12Viewer.Domain;

public sealed class X835Document
{
    public string IsaRawText { get; init; } = "";
    public bool HasBpr { get; init; }
    public List<X835Claim> Claims { get; init; } = new();
}
