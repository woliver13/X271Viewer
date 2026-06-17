using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public class X835Validator
{
    public IReadOnlyList<string> Validate(X835Document doc)
    {
        var errors = new List<string>();

        if (!doc.HasBpr)
            errors.Add("BPR segment is required but missing.");

        return errors;
    }
}
