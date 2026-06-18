namespace woliver13.X12Viewer.Domain;

public sealed record X270ServiceTypeQuery(string ServiceTypeCode, string? Description = null);

public sealed record X270Dependent(string PatientName, IReadOnlyList<X270ServiceTypeQuery> ServiceTypeQueries);

public sealed record X270Subscriber(
    string SubscriberId,
    string SubscriberName,
    IReadOnlyList<X270ServiceTypeQuery> ServiceTypeQueries,
    X270Dependent? Dependent = null);

public sealed class X270Document
{
    public List<X270Subscriber> Subscribers { get; init; } = [];
}
