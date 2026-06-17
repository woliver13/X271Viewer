using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Viewer.Domain;

public sealed class X277DocumentParser
{
    public X277Document ParseFile(string filePath)
    {
        string content;
        try { content = File.ReadAllText(filePath); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
        {
            throw new X271ParseException($"Cannot read file: {filePath}", ex);
        }
        return ParseContent(content);
    }

    public X277Document ParseContent(string content)
    {
        List<X12Segment> segments;
        try
        {
            using var reader = new X12Reader(content);
            segments = reader.ReadAllSegments().ToList();
        }
        catch (Exception ex) when (ex is not X271ParseException)
        {
            throw new X271ParseException("Failed to parse X12 interchange.", ex);
        }

        var doc = new X277Document();
        BuildClaims(segments, doc.Claims);
        return doc;
    }

    private static void BuildClaims(List<X12Segment> segments, List<X277Claim> claims)
    {
        X277Claim? current = null;

        foreach (var seg in segments)
        {
            switch (seg.SegmentId)
            {
                case "TRN" when SafeGet(seg, 1) == "2":
                    if (current != null) claims.Add(current);
                    current = new X277Claim();
                    break;

                case "STC" when current != null:
                    var composite = SafeGet(seg, 1);
                    var parts = composite.Split(':');
                    current.StatusCategoryCode = parts.Length > 0 ? parts[0] : "";
                    current.StatusCode = parts.Length > 1 ? parts[1] : "";
                    break;

                case "REF" when current != null && SafeGet(seg, 1) == "1K":
                    current.ClaimId = SafeGet(seg, 2);
                    break;
            }
        }

        if (current != null) claims.Add(current);
    }

    private static string SafeGet(X12Segment seg, int index)
    {
        var els = seg.Elements;
        return index <= els.Count ? els[index - 1] : "";
    }
}
