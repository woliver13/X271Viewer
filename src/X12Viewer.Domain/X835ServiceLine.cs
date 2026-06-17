namespace woliver13.X12Viewer.Domain;

public sealed class X835ServiceLine
{
    public string ProcedureCode { get; init; } = "";
    public decimal BilledAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public List<X835Adjustment> Adjustments { get; init; } = new();
}
