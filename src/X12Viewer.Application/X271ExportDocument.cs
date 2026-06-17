namespace woliver13.X12Viewer.Application;

public sealed record X271ExportNode(
    string Label,
    IReadOnlyList<string> RawSegments,
    string Interpretation,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<X271ExportNode> Children);

public sealed record X271ExportDocument(
    string IsaRawText,
    X271ExportNode Root,
    IReadOnlyList<X271ExportValidationError> ValidationResults);

public sealed record X271ExportValidationError(string Code, string Message);
