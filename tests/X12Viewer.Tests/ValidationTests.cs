using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class ValidationTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void X271ValidationError_Has_Code_And_Message()
    {
        var error = new X271ValidationError("EbMissingEligibilityCode", "EB01 is missing.");
        Assert.Equal("EbMissingEligibilityCode", error.Code);
        Assert.Equal("EB01 is missing.", error.Message);
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void X271ValidationResult_IsValid_True_When_No_Errors()
    {
        var result = new X271ValidationResult([]);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void X271ValidationResult_IsValid_False_When_Has_Errors()
    {
        var error = new X271ValidationError("EbMissingEligibilityCode", "EB01 is missing.");
        var result = new X271ValidationResult([error]);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    // ── Cycles 3 & 4 ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_WellFormed271_Returns_IsValid()
    {
        var content = File.ReadAllText(Path.Combine(FixtureDir, "valid271.edi"));
        var svc = new X271ValidationService();
        var result = svc.Validate(content);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ToString())));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MalformedEB_Returns_Errors()
    {
        var content = File.ReadAllText(Path.Combine(FixtureDir, "malformed271.edi"));
        var svc = new X271ValidationService();
        var result = svc.Validate(content);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // ── Cycle 5 ───────────────────────────────────────────────────────────

    [Fact]
    public void X271Node_Has_ValidationErrors_Empty_By_Default()
    {
        var node = new X271Node("Test", [], []);
        Assert.Empty(node.ValidationErrors);
    }

    // ── Cycle 6 ───────────────────────────────────────────────────────────

    [Fact]
    public void AnnotateTree_Assigns_EB_Errors_To_Nodes_With_EB_Segment()
    {
        var content = File.ReadAllText(Path.Combine(FixtureDir, "malformed271.edi"));
        var parser  = new woliver13.X12Viewer.Domain.X271DocumentParser();
        var doc     = parser.ParseContent(content);
        var root    = woliver13.X12Viewer.Domain.X271TreeBuilder.Build(doc);

        var svc    = new X271ValidationService();
        var result = svc.Validate(content);
        svc.AnnotateTree(root, result);

        // At least one node should have validation errors (the EB node with blank EB01)
        bool anyNodeHasErrors = AnyNodeHasErrors(root);
        Assert.True(anyNodeHasErrors);
    }

    private static bool AnyNodeHasErrors(woliver13.X12Viewer.Domain.X271Node node)
    {
        if (node.HasValidationErrors) return true;
        return node.Children.Any(AnyNodeHasErrors);
    }
}
