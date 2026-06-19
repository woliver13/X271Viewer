using woliver13.X12Viewer.Domain;
using woliver13.X12Viewer.Application;

namespace woliver13.X12Viewer.Tests;

public class X837PInterpreterTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private X837PDocument ParseFixture() =>
        new X837PDocumentParser().ParseFile(Path.Combine(FixtureDir, "tests837p.edi"));

    // Criterion: X12CodeTable.Resolve for a known ICD-10-CM code returns a non-empty description
    [Fact]
    public void CodeTable_KnownIcd10Cm_ReturnsNonEmptyDescription()
    {
        var result = X12CodeTable.Resolve("ICD10CM", "J06.9");
        Assert.NotEmpty(result);
        Assert.DoesNotContain("unrecognized", result);
    }

    // Criterion: X12CodeTable.Resolve for a known HCPCS Level II code returns a non-empty description
    [Fact]
    public void CodeTable_KnownHcpcs_ReturnsNonEmptyDescription()
    {
        var result = X12CodeTable.Resolve("HCPCS", "G0008");
        Assert.NotEmpty(result);
        Assert.DoesNotContain("unrecognized", result);
    }

    // Criterion: Unknown ICD-10 code falls back to "{code} (unrecognized code)"
    [Fact]
    public void CodeTable_UnknownIcd10_FallsBackToUnrecognizedFormat()
    {
        var result = X12CodeTable.Resolve("ICD10CM", "ZZZZ.99");
        Assert.Equal("ZZZZ.99 (unrecognized code)", result);
    }

    // Criterion: X837PInterpreter.Interpret populates diagnosis descriptions on each claim
    [Fact]
    public void Interpret_PopulatesDiagnosisDescriptions()
    {
        var doc      = ParseFixture();
        var enriched = X837PInterpreter.Interpret(doc);
        var dx       = enriched.Claims[0].DiagnosisCodes[0];
        Assert.NotNull(dx.Description);
        Assert.NotEmpty(dx.Description!);
    }

    // Criterion: CPT procedure code is passed through as-is without a description lookup
    [Fact]
    public void Interpret_CptCode_HasNullProcedureDescription()
    {
        var doc      = ParseFixture();
        var enriched = X837PInterpreter.Interpret(doc);
        // 99213 is a CPT code (5-digit numeric) — should not get a description
        var svc = enriched.Claims[0].ServiceLines[0];
        Assert.Equal("99213", svc.ProcedureCode);
        Assert.Null(svc.ProcedureDescription);
    }
}
