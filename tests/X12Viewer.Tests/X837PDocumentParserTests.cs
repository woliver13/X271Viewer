using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X837PDocumentParserTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private X837PDocument ParseFixture() =>
        new X837PDocumentParser().ParseFile(FixturePath("tests837p.edi"));

    [Fact]
    public void ParseFile_ReturnsCorrectClaimCount()
    {
        var doc = ParseFixture();
        Assert.Equal(2, doc.Claims.Count);
    }

    // Criterion: X837BillingProvider exposes NPI and Name
    [Fact]
    public void BillingProvider_ExposesNpiAndName()
    {
        var doc = ParseFixture();
        Assert.Equal("1234567890", doc.BillingProvider.Npi);
        Assert.Equal("METRO CLINIC", doc.BillingProvider.Name);
    }

    // Criterion: X837Subscriber exposes SubscriberId and SubscriberName
    [Fact]
    public void Subscriber_ExposesSubscriberIdAndName()
    {
        var doc = ParseFixture();
        // First claim is under subscriber 1 (JANE DOE)
        Assert.Equal("123456789A", doc.Claims[0].Subscriber.SubscriberId);
        Assert.Equal("JANE DOE", doc.Claims[0].Subscriber.SubscriberName);
        // Second claim is under subscriber 2 (JOHN SMITH)
        Assert.Equal("987654321B", doc.Claims[1].Subscriber.SubscriberId);
        Assert.Equal("JOHN SMITH", doc.Claims[1].Subscriber.SubscriberName);
    }

    // Criterion: Dependent 2000C loop populates X837Patient with PatientName distinct from subscriber
    [Fact]
    public void DependentLoop_PopulatesPatientName()
    {
        var doc = ParseFixture();
        // Claim 1 has a dependent patient (BABY DOE)
        Assert.NotNull(doc.Claims[0].Patient);
        Assert.Equal("BABY DOE", doc.Claims[0].Patient!.PatientName);
        // Claim 2 has no dependent
        Assert.Null(doc.Claims[1].Patient);
    }

    // Criterion: Each X837Claim exposes ClaimId, PlaceOfService, BilledAmount, and at least one diagnosis code
    [Fact]
    public void Claim_ExposesRequiredFields()
    {
        var doc = ParseFixture();
        var claim1 = doc.Claims[0];
        Assert.Equal("CLAIM001", claim1.ClaimId);
        Assert.Equal("11", claim1.PlaceOfService);
        Assert.Equal(100.00m, claim1.BilledAmount);
        Assert.NotEmpty(claim1.DiagnosisCodes);

        var claim2 = doc.Claims[1];
        Assert.Equal("CLAIM002", claim2.ClaimId);
        Assert.NotEmpty(claim2.DiagnosisCodes);
    }

    // Criterion: Each X837PServiceLine exposes ProcedureCode, Modifier, Units, BilledAmount
    [Fact]
    public void ServiceLine_ExposesRequiredFields()
    {
        var doc = ParseFixture();
        var svc = doc.Claims[0].ServiceLines[0];
        Assert.Equal("99213", svc.ProcedureCode);
        Assert.Equal(75.00m, svc.BilledAmount);
        Assert.Equal(1m, svc.Units);
    }
}
