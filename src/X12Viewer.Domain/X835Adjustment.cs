namespace woliver13.X12Viewer.Domain;

public sealed class X835Adjustment
{
    public string GroupCode { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public decimal AdjustmentAmount { get; init; }
}
