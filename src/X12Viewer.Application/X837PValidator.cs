using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public sealed class X837PValidator
{
    public IReadOnlyList<string> Validate(X837PDocument doc)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(doc.BillingProvider.Npi))
            errors.Add("Billing provider NPI (NM1*85 element 9) is required but missing.");

        foreach (var claim in doc.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.ClaimId))
                errors.Add("CLM01 (claim ID) is required but missing.");

            if (claim.DiagnosisCodes.Count == 0)
                errors.Add($"Claim '{claim.ClaimId}': at least one HI diagnosis segment is required.");

            if (claim.ServiceLines.Count == 0)
                errors.Add($"Claim '{claim.ClaimId}': at least one SV1 service line is required.");
        }

        if (doc.Claims.Count == 0)
            errors.Add("No CLM segments found — at least one claim is required.");

        return errors;
    }
}
