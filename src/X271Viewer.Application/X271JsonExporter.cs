using System.Text.Json;
using woliver13.X271Viewer.Domain;

namespace woliver13.X271Viewer.Application;

public static class X271JsonExporter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Export(X271ExportDocument doc) =>
        JsonSerializer.Serialize(doc, Options);

    public static X271ExportDocument Import(string json) =>
        JsonSerializer.Deserialize<X271ExportDocument>(json, Options)!;

    public static X271ExportDocument BuildExportDocument(
        X271Node root, X271ValidationResult validationResult, string isaRawText = "")
    {
        return new X271ExportDocument(
            IsaRawText: isaRawText,
            Root: BuildNode(root),
            ValidationResults: validationResult.Errors
                .Select(e => new X271ExportValidationError(e.Code, e.Message))
                .ToList());
    }

    private static X271ExportNode BuildNode(X271Node node) =>
        new(
            Label: node.Label,
            RawSegments: node.RawSegments.ToList(),
            Interpretation: X271InterpretationEngine.Interpret(node),
            ValidationErrors: node.ValidationErrors.ToList(),
            Children: node.Children.Select(BuildNode).ToList());
}
