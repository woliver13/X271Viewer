using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X837PValidatorTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private X837PDocument ParseFixture() =>
        new X837PDocumentParser().ParseFile(Path.Combine(FixtureDir, "tests837p.edi"));

    // Criterion: X837PValidator.Validate returns zero errors for well-formed fixture
    [Fact]
    public void Validate_WellFormed_ReturnsNoErrors()
    {
        var doc    = ParseFixture();
        var errors = new X837PValidator().Validate(doc);
        Assert.Empty(errors);
    }

    // Criterion: Missing CLM01 produces a validation error
    [Fact]
    public void Validate_MissingClaimId_ReturnsError()
    {
        var doc = new X837PDocument
        {
            BillingProvider = new X837BillingProvider { Npi = "1234567890", Name = "Test" },
            Claims =
            [
                new X837Claim
                {
                    ClaimId        = "",   // missing
                    DiagnosisCodes = [ new X837DiagnosisCode("J06.9") ],
                    ServiceLines   = [ new X837PServiceLine { ProcedureCode = "99213", BilledAmount = 75m, Units = 1m } ],
                }
            ],
        };
        var errors = new X837PValidator().Validate(doc);
        Assert.Contains(errors, e => e.Contains("CLM01"));
    }

    // Criterion: Missing billing provider NPI produces a validation error
    [Fact]
    public void Validate_MissingBillingProviderNpi_ReturnsError()
    {
        var doc = new X837PDocument
        {
            BillingProvider = new X837BillingProvider { Npi = "", Name = "Test" },
            Claims =
            [
                new X837Claim
                {
                    ClaimId        = "C001",
                    DiagnosisCodes = [ new X837DiagnosisCode("J06.9") ],
                    ServiceLines   = [ new X837PServiceLine { ProcedureCode = "99213", BilledAmount = 75m, Units = 1m } ],
                }
            ],
        };
        var errors = new X837PValidator().Validate(doc);
        Assert.Contains(errors, e => e.Contains("NPI") || e.Contains("billing provider"));
    }

    // Criterion: Missing HI diagnosis segment produces a validation error
    [Fact]
    public void Validate_MissingDiagnosis_ReturnsError()
    {
        var doc = new X837PDocument
        {
            BillingProvider = new X837BillingProvider { Npi = "1234567890", Name = "Test" },
            Claims =
            [
                new X837Claim
                {
                    ClaimId        = "C001",
                    DiagnosisCodes = [],   // missing
                    ServiceLines   = [ new X837PServiceLine { ProcedureCode = "99213", BilledAmount = 75m, Units = 1m } ],
                }
            ],
        };
        var errors = new X837PValidator().Validate(doc);
        Assert.Contains(errors, e => e.Contains("HI") || e.Contains("diagnosis"));
    }

    // Criterion: Missing SV1 service line produces a validation error
    [Fact]
    public void Validate_MissingServiceLine_ReturnsError()
    {
        var doc = new X837PDocument
        {
            BillingProvider = new X837BillingProvider { Npi = "1234567890", Name = "Test" },
            Claims =
            [
                new X837Claim
                {
                    ClaimId        = "C001",
                    DiagnosisCodes = [ new X837DiagnosisCode("J06.9") ],
                    ServiceLines   = [],   // missing
                }
            ],
        };
        var errors = new X837PValidator().Validate(doc);
        Assert.Contains(errors, e => e.Contains("SV1") || e.Contains("service line"));
    }
}
