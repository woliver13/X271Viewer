using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X837PInterpreter
{
    // CPT codes are 5-digit numeric (10000–99999); HCPCS Level II start with a letter.
    private static bool IsCptCode(string code) =>
        code.Length == 5 && code.All(char.IsDigit);

    public static X837PDocument Interpret(X837PDocument doc)
    {
        var enrichedClaims = doc.Claims.Select(EnrichClaim).ToList();
        return new X837PDocument
        {
            BillingProvider = doc.BillingProvider,
            Claims          = enrichedClaims,
        };
    }

    private static X837Claim EnrichClaim(X837Claim claim)
    {
        var enrichedDx  = claim.DiagnosisCodes.Select(EnrichDx).ToList();
        var enrichedSvc = claim.ServiceLines.Select(EnrichServiceLine).ToList();
        return new X837Claim
        {
            ClaimId        = claim.ClaimId,
            PlaceOfService = claim.PlaceOfService,
            BilledAmount   = claim.BilledAmount,
            Subscriber     = claim.Subscriber,
            Patient        = claim.Patient,
            DiagnosisCodes = enrichedDx,
            ServiceLines   = enrichedSvc,
        };
    }

    private static X837DiagnosisCode EnrichDx(X837DiagnosisCode dx) =>
        dx with { Description = X12CodeTable.Resolve("ICD10CM", dx.Code) };

    private static X837PServiceLine EnrichServiceLine(X837PServiceLine svc)
    {
        var desc = IsCptCode(svc.ProcedureCode)
            ? null
            : X12CodeTable.Resolve("HCPCS", svc.ProcedureCode);

        return new X837PServiceLine
        {
            ProcedureCode        = svc.ProcedureCode,
            Modifier             = svc.Modifier,
            Units                = svc.Units,
            BilledAmount         = svc.BilledAmount,
            ProcedureDescription = desc,
        };
    }
}
