using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X837PTreeBuilder
{
    public static X271Node Build(X837PDocument doc)
    {
        // Group claims by subscriber (by ID) to build provider → subscriber → claim hierarchy
        var claimsBySubscriber = doc.Claims
            .GroupBy(c => c.Subscriber.SubscriberId)
            .ToList();

        var subscriberNodes = claimsBySubscriber.Select(BuildSubscriberNode).ToList();

        var providerLabel = $"Billing Provider: {doc.BillingProvider.Name} (NPI: {doc.BillingProvider.Npi})";
        return new X271Node(providerLabel, [], subscriberNodes);
    }

    private static X271Node BuildSubscriberNode(IGrouping<string, X837Claim> group)
    {
        var first = group.First();
        var subLabel = $"Subscriber: {first.Subscriber.SubscriberName} ({first.Subscriber.SubscriberId})";

        // If a dependent patient exists, wrap claims under a patient node
        var patientClaims  = group.Where(c => c.Patient is not null).ToList();
        var directClaims   = group.Where(c => c.Patient is null).ToList();

        var children = new List<X271Node>();

        if (patientClaims.Count > 0)
        {
            var patientName = patientClaims[0].Patient!.PatientName;
            var claimNodes  = patientClaims.Select(BuildClaimNode).ToList();
            children.Add(new X271Node($"Patient: {patientName}", [], claimNodes));
        }

        children.AddRange(directClaims.Select(BuildClaimNode));

        return new X271Node(subLabel, [], children);
    }

    private static X271Node BuildClaimNode(X837Claim claim)
    {
        var label = $"Claim: {claim.ClaimId} — ${claim.BilledAmount:F2}";

        var dxNodes  = claim.DiagnosisCodes.Select(BuildDxNode).ToList();
        var svcNodes = claim.ServiceLines.Select(BuildServiceLineNode).ToList();

        return new X271Node(label, [], [.. dxNodes, .. svcNodes]);
    }

    private static X271Node BuildDxNode(X837DiagnosisCode dx)
    {
        var label = string.IsNullOrEmpty(dx.Description)
            ? $"Diagnosis: {dx.Code}"
            : $"Diagnosis: {dx.Code} — {dx.Description}";
        return new X271Node(label, [], []);
    }

    private static X271Node BuildServiceLineNode(X837PServiceLine svc)
    {
        var label = $"SV1: {svc.ProcedureCode} — ${svc.BilledAmount:F2}";
        return new X271Node(label, [], []);
    }
}
