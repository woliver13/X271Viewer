namespace woliver13.X12Viewer.Domain;

public sealed class X837BillingProvider
{
    public string Npi  { get; init; } = "";
    public string Name { get; init; } = "";
}

public sealed class X837Subscriber
{
    public string SubscriberId   { get; init; } = "";
    public string SubscriberName { get; init; } = "";
}

public sealed class X837Patient
{
    public string PatientName { get; init; } = "";
}

public sealed record X837DiagnosisCode(string Code, string? Description = null);

public sealed class X837PServiceLine
{
    public string  ProcedureCode        { get; init; } = "";
    public string  Modifier             { get; init; } = "";
    public decimal Units                { get; init; }
    public decimal BilledAmount         { get; init; }
    public string? ProcedureDescription { get; init; }
}

public sealed class X837Claim
{
    public string                  ClaimId        { get; init; } = "";
    public string                  PlaceOfService { get; init; } = "";
    public decimal                 BilledAmount   { get; init; }
    public X837Subscriber          Subscriber     { get; init; } = new();
    public X837Patient?            Patient        { get; init; }
    public List<X837DiagnosisCode> DiagnosisCodes { get; init; } = new();
    public List<X837PServiceLine>  ServiceLines   { get; init; } = new();
}

public sealed class X837PDocument
{
    public X837BillingProvider BillingProvider { get; init; } = new();
    public List<X837Claim>     Claims          { get; init; } = new();
}
