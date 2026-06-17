namespace woliver13.X12Viewer.Domain;

public sealed class X835Claim
{
    public string ClaimId { get; set; } = "";
    public string RawBilledAmount { get; set; } = "";
    public string PatientName { get; set; } = "";
    public decimal BilledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string ClaimStatusCode { get; set; } = "";
    public string? RarcRemarkCode { get; set; }
    public string? RarcRemarkDescription { get; init; }
    public List<X835ServiceLine> ServiceLines { get; init; } = new();
}
