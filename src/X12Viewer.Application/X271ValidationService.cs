using woliver13.X12Viewer.Domain;
using woliver13.X12Net.Validation;

namespace woliver13.X12Viewer.Application;

public sealed class X271ValidationService
{
    public X271ValidationResult Validate(string content)
    {
        var x12Result = Ts271Validator.Validate(content);
        var errors = x12Result.Errors
            .Select(e => new X271ValidationError(e.Code.ToString(), e.Message))
            .ToList();
        return new X271ValidationResult(errors);
    }

    public void AnnotateTree(X271Node root, X271ValidationResult result)
    {
        AnnotateNode(root);
    }

    private static void AnnotateNode(X271Node node)
    {
        foreach (var raw in node.RawSegments)
        {
            var trimmed = raw.TrimStart();
            if (!trimmed.StartsWith("EB", StringComparison.Ordinal)) continue;

            var ebResult = EbSegmentValidator.ValidateRaw(trimmed);
            foreach (var err in ebResult.Errors)
                node.ValidationErrors.Add(err.Message);
        }

        foreach (var child in node.Children)
            AnnotateNode(child);
    }
}
