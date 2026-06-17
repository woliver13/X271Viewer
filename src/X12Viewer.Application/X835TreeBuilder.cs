using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X835TreeBuilder
{
    public static X271Node Build(X835Document doc)
    {
        var enriched   = X835Interpreter.Interpret(doc);
        var claimNodes = enriched.Claims.Select(BuildClaimNode).ToList();
        var gsNode     = new X271Node("GS — Payment Group", [], claimNodes, isCollapsedByDefault: true);
        return new X271Node("835 — Remittance Advice", [], [gsNode]);
    }

    private static X271Node BuildClaimNode(X835Claim claim)
    {
        var statusText = claim.ClaimStatusCode switch
        {
            "1" => "Paid",
            "2" => "Adjusted",
            "3" => "Acknowledged",
            "4" => "Denied",
            var c => $"Status {c}"
        };
        var label = $"{claim.PatientName} — Billed: {claim.BilledAmount:C} / Paid: {claim.PaidAmount:C} — {statusText}";

        var children = claim.ServiceLines.Select(BuildServiceLineNode).ToList<X271Node>();

        if (claim.RarcRemarkCode is not null)
        {
            var rarcLabel = $"RARC {claim.RarcRemarkCode}: {claim.RarcRemarkDescription ?? claim.RarcRemarkCode}";
            children.Add(new X271Node(rarcLabel, [], []));
        }

        return new X271Node(label, [], children);
    }

    private static X271Node BuildServiceLineNode(X835ServiceLine line)
    {
        var label    = $"SVC {line.ProcedureCode} — Billed: {line.BilledAmount:C} / Paid: {line.PaidAmount:C}";
        var adjNodes = line.Adjustments.Select(BuildAdjustmentNode).ToList<X271Node>();
        return new X271Node(label, [], adjNodes);
    }

    private static X271Node BuildAdjustmentNode(X835Adjustment adj)
    {
        var desc  = adj.ReasonDescription ?? adj.ReasonCode;
        var label = $"CAS {adj.GroupCode}-{adj.ReasonCode}: {desc} ({adj.AdjustmentAmount:C})";
        return new X271Node(label, [], []);
    }
}
