using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X270Interpreter
{
    public static X270Document Interpret(X270Document doc)
    {
        var enriched = doc.Subscribers.Select(EnrichSubscriber).ToList();
        return new X270Document { Subscribers = enriched };
    }

    private static X270Subscriber EnrichSubscriber(X270Subscriber sub)
    {
        var eqs = sub.ServiceTypeQueries.Select(EnrichEq).ToList();
        var dep = sub.Dependent is not null ? EnrichDependent(sub.Dependent) : null;
        return sub with { ServiceTypeQueries = eqs, Dependent = dep };
    }

    private static X270Dependent EnrichDependent(X270Dependent dep)
    {
        var eqs = dep.ServiceTypeQueries.Select(EnrichEq).ToList();
        return dep with { ServiceTypeQueries = eqs };
    }

    private static X270ServiceTypeQuery EnrichEq(X270ServiceTypeQuery eq)
        => eq with { Description = X12CodeTable.Resolve("EQ01", eq.ServiceTypeCode) };
}
