using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X835Interpreter
{
    public static X835Document Interpret(X835Document doc)
    {
        var enrichedClaims = doc.Claims.Select(EnrichClaim).ToList();
        return new X835Document { IsaRawText = doc.IsaRawText, Claims = enrichedClaims };
    }

    private static X835Claim EnrichClaim(X835Claim claim)
    {
        var enrichedLines = claim.ServiceLines.Select(EnrichServiceLine).ToList();
        string? remarkDesc = claim.RarcRemarkCode is not null
            ? X12CodeTable.Resolve("MOA09", claim.RarcRemarkCode)
            : null;
        return new X835Claim
        {
            ClaimId = claim.ClaimId,
            PatientName = claim.PatientName,
            BilledAmount = claim.BilledAmount,
            PaidAmount = claim.PaidAmount,
            ClaimStatusCode = claim.ClaimStatusCode,
            RarcRemarkCode = claim.RarcRemarkCode,
            RarcRemarkDescription = remarkDesc,
            ServiceLines = enrichedLines
        };
    }

    private static X835ServiceLine EnrichServiceLine(X835ServiceLine line)
    {
        var enrichedAdjs = line.Adjustments.Select(EnrichAdjustment).ToList();
        return new X835ServiceLine
        {
            ProcedureCode = line.ProcedureCode,
            BilledAmount = line.BilledAmount,
            PaidAmount = line.PaidAmount,
            Adjustments = enrichedAdjs
        };
    }

    private static X835Adjustment EnrichAdjustment(X835Adjustment adj)
    {
        return new X835Adjustment
        {
            GroupCode = adj.GroupCode,
            ReasonCode = adj.ReasonCode,
            AdjustmentAmount = adj.AdjustmentAmount,
            ReasonDescription = X12CodeTable.Resolve("CAS02", adj.ReasonCode)
        };
    }
}
