using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X277Interpreter
{
    public static X277Document Interpret(X277Document doc)
    {
        var result = new X277Document();
        foreach (var claim in doc.Claims)
            result.Claims.Add(Enrich(claim));
        return result;
    }

    private static X277Claim Enrich(X277Claim claim)
    {
        var desc = X12CodeTable.Resolve("STC01-1", claim.StatusCategoryCode);
        return new X277Claim
        {
            ClaimId = claim.ClaimId,
            StatusCategoryCode = claim.StatusCategoryCode,
            StatusCode = claim.StatusCode,
            StatusDescription = desc
        };
    }
}
