namespace woliver13.X12Viewer.Domain;

public sealed class X277Claim
{
    public string ClaimId { get; set; } = "";
    public string StatusCategoryCode { get; set; } = "";
    public string StatusCode { get; set; } = "";
    public string? StatusDescription { get; set; }
}
