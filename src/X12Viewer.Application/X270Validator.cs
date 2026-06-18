using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Viewer.Application;

public static class X270Validator
{
    public static IReadOnlyList<string> Validate(string content)
    {
        var errors = new List<string>();

        List<X12Segment> segments;
        try
        {
            using var reader = new X12Reader(content);
            segments = reader.ReadAllSegments().ToList();
        }
        catch
        {
            errors.Add("Failed to parse X12 interchange.");
            return errors;
        }

        bool hasSubscriberLoop = segments.Any(s =>
            s.SegmentId == "HL" &&
            s.Elements.Count >= 3 &&
            s.Elements[2] == "22");

        if (!hasSubscriberLoop)
            errors.Add("270 must contain at least one 2000B subscriber loop (HL level 22).");

        bool hasEq = segments.Any(s => s.SegmentId == "EQ");
        if (!hasEq)
            errors.Add("270 must contain at least one EQ service type query segment.");

        return errors;
    }
}
