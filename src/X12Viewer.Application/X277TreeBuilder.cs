using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X277TreeBuilder
{
    private static readonly IReadOnlyList<string> NoSegments = [];

    public static X271Node Build(X277Document doc)
    {
        var claimNodes = doc.Claims.Select(BuildClaimNode).ToList();
        return new X271Node("277 Claim Status Response", NoSegments, claimNodes, isCollapsedByDefault: false);
    }

    private static X271Node BuildClaimNode(X277Claim claim)
    {
        var desc = claim.StatusDescription ?? claim.StatusCategoryCode;
        var label = string.IsNullOrEmpty(claim.ClaimId)
            ? desc
            : $"{claim.ClaimId} — {desc}";

        var children = new List<X271Node>
        {
            new($"Category: {claim.StatusCategoryCode} — {desc}", NoSegments, [])
        };

        if (!string.IsNullOrEmpty(claim.StatusCode))
            children.Add(new X271Node($"Status Code: {claim.StatusCode}", NoSegments, []));

        return new X271Node(label, NoSegments, children, isCollapsedByDefault: false);
    }
}
