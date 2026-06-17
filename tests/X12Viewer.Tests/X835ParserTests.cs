using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X835ParserTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private X835Document ParseFixture() =>
        new X835DocumentParser().ParseFile(FixturePath("tests835.edi"));

    // ── Cycle 1: claim count ──────────────────────────────────────────────────

    [Fact]
    public void X835Document_ParseFile_returns_correct_claim_count()
    {
        var doc = ParseFixture();
        Assert.Equal(4, doc.Claims.Count);
    }

    // ── Cycle 2: ClaimId and ClaimStatusCode ─────────────────────────────────

    [Theory]
    [InlineData(0, "CLM-001", "1")]
    [InlineData(1, "CLM-002", "2")]
    [InlineData(2, "CLM-003", "4")]
    [InlineData(3, "CLM-004", "4")]
    public void X835Claim_has_correct_ClaimId_and_status(int idx, string claimId, string status)
    {
        var doc = ParseFixture();
        var claim = doc.Claims[idx];
        Assert.Equal(claimId, claim.ClaimId);
        Assert.Equal(status, claim.ClaimStatusCode);
    }

    // ── Cycle 3: BilledAmount and PaidAmount ──────────────────────────────────

    [Theory]
    [InlineData(0, 100.00, 100.00)]
    [InlineData(1, 200.00, 155.00)]
    [InlineData(2, 300.00, 0.00)]
    [InlineData(3, 150.00, 0.00)]
    public void X835Claim_has_correct_billed_and_paid_amounts(int idx, double billed, double paid)
    {
        var doc = ParseFixture();
        var claim = doc.Claims[idx];
        Assert.Equal((decimal)billed, claim.BilledAmount);
        Assert.Equal((decimal)paid,   claim.PaidAmount);
    }

    // ── Cycle 4: PatientName from NM1*QC ─────────────────────────────────────

    [Theory]
    [InlineData(0, "JANE DOE")]
    [InlineData(1, "JOHN SMITH")]
    [InlineData(2, "MARY JONES")]
    [InlineData(3, "ROBERT BROWN")]
    public void X835Claim_has_correct_PatientName_from_NM1(int idx, string expected)
    {
        var doc = ParseFixture();
        Assert.Equal(expected, doc.Claims[idx].PatientName);
    }

    // ── Cycle 5: ServiceLine ProcedureCode / amounts ─────────────────────────

    [Theory]
    [InlineData(0, "99213", 100.00, 100.00)]
    [InlineData(1, "99214", 200.00, 155.00)]
    [InlineData(2, "99215", 300.00, 0.00)]
    [InlineData(3, "99212", 150.00, 0.00)]
    public void X835ServiceLine_has_ProcedureCode_billed_paid(int claimIdx, string procCode, double billed, double paid)
    {
        var doc = ParseFixture();
        var svc = doc.Claims[claimIdx].ServiceLines[0];
        Assert.Equal(procCode,       svc.ProcedureCode);
        Assert.Equal((decimal)billed, svc.BilledAmount);
        Assert.Equal((decimal)paid,   svc.PaidAmount);
    }

    // ── Cycle 6: Adjustment GroupCode / ReasonCode / Amount ──────────────────

    [Theory]
    [InlineData(1, "CO", "45", 45.00)]
    [InlineData(2, "CO", "50", 300.00)]
    [InlineData(3, "CO", "50", 150.00)]
    public void X835Adjustment_has_GroupCode_ReasonCode_Amount(int claimIdx, string groupCode, string reasonCode, double amount)
    {
        var doc = ParseFixture();
        var adj = doc.Claims[claimIdx].ServiceLines[0].Adjustments[0];
        Assert.Equal(groupCode,       adj.GroupCode);
        Assert.Equal(reasonCode,      adj.ReasonCode);
        Assert.Equal((decimal)amount, adj.AdjustmentAmount);
    }

    // ── Cycle 7: RARC remark code from MOA09 ─────────────────────────────────

    [Fact]
    public void X835Claim_has_RARC_remark_code_from_MOA()
    {
        var doc = ParseFixture();
        // Claim index 3 (CLM-004) has MOA*****MA01
        Assert.Equal("MA01", doc.Claims[3].RarcRemarkCode);
    }

    [Fact]
    public void X835Claim_without_MOA_has_null_RarcRemarkCode()
    {
        var doc = ParseFixture();
        // Claim 0 has no MOA segment
        Assert.Null(doc.Claims[0].RarcRemarkCode);
    }
}
