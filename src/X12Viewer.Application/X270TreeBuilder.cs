using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X270TreeBuilder
{
    public static X271Node Build(X270Document doc)
    {
        var subscriberNodes = doc.Subscribers.Select(BuildSubscriberNode).ToList();
        return new X271Node("270 Eligibility Inquiry", [], subscriberNodes);
    }

    private static X271Node BuildSubscriberNode(X270Subscriber sub)
    {
        var eqNodes = sub.ServiceTypeQueries
            .Select(eq => new X271Node(EqLabel(eq), [], []))
            .ToList<X271Node>();

        var children = new List<X271Node>(eqNodes);

        if (sub.Dependent is not null)
            children.Add(BuildDependentNode(sub.Dependent));

        return new X271Node($"Subscriber: {sub.SubscriberName} ({sub.SubscriberId})", [], children);
    }

    private static X271Node BuildDependentNode(X270Dependent dep)
    {
        var eqNodes = dep.ServiceTypeQueries
            .Select(eq => new X271Node(EqLabel(eq), [], []))
            .ToList<X271Node>();

        return new X271Node($"Dependent: {dep.PatientName}", [], eqNodes);
    }

    private static string EqLabel(X270ServiceTypeQuery eq)
        => string.IsNullOrEmpty(eq.Description)
            ? eq.ServiceTypeCode
            : $"{eq.ServiceTypeCode} — {eq.Description}";
}
