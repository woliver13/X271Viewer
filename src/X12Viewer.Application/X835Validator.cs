using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public class X835Validator
{
    public IReadOnlyList<string> Validate(X835Document doc)
    {
        var errors = new List<string>();

        if (!doc.HasBpr)
            errors.Add("BPR segment is required but missing.");

        var validGroupCodes = new HashSet<string> { "CO", "OA", "PI", "PR" };
        foreach (var claim in doc.Claims)
            foreach (var svc in claim.ServiceLines)
                foreach (var adj in svc.Adjustments)
                    if (!validGroupCodes.Contains(adj.GroupCode))
                        errors.Add($"CAS adjustment has invalid group code '{adj.GroupCode}'. Valid codes: CO, OA, PI, PR.");

        return errors;
    }
}
