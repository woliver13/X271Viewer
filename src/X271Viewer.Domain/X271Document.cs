using woliver13.X12Net.Core;

namespace X271Viewer.Domain;

public sealed class X271Document
{
    public string IsaRawText { get; init; } = string.Empty;
    public IReadOnlyList<X12Segment> Segments { get; init; } = [];
    public X12Delimiters Delimiters { get; init; } = new X12Delimiters('*', ':', '~');
}
