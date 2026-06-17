using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Viewer.Domain;

public sealed class X835DocumentParser
{
    public X835Document ParseFile(string filePath)
    {
        string content;
        try { content = File.ReadAllText(filePath); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
        {
            throw new X271ParseException($"Cannot read file: {filePath}", ex);
        }
        return ParseContent(content);
    }

    public X835Document ParseContent(string content)
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

        var isa = segments.FirstOrDefault(s => s.SegmentId == "ISA")
            ?? throw new X271ParseException("No ISA segment found — not a valid X12 interchange.");

        var end = content.IndexOf('~');
        var isaRaw = end >= 0 ? content[..(end + 1)] : content;

        var doc = new X835Document { IsaRawText = isaRaw };
        BuildClaims(segments, doc.Claims);
        return doc;
    }

    private static void BuildClaims(List<X12Segment> segments, List<X835Claim> claims)
    {
        X835Claim? currentClaim = null;
        X835ServiceLine? currentSvc = null;

        foreach (var seg in segments)
        {
            switch (seg.SegmentId)
            {
                case "CLP":
                    // Flush previous claim
                    if (currentClaim != null)
                    {
                        if (currentSvc != null) { currentClaim.ServiceLines.Add(currentSvc); currentSvc = null; }
                        claims.Add(currentClaim);
                    }
                    currentClaim = new X835Claim
                    {
                        ClaimId         = SafeGet(seg, 1),
                        ClaimStatusCode = SafeGet(seg, 2),
                        BilledAmount    = ParseDecimal(SafeGet(seg, 3)),
                        PaidAmount      = ParseDecimal(SafeGet(seg, 4)),
                        // PatientName filled by subsequent NM1*QC
                    };
                    currentSvc = null;
                    break;

                case "NM1" when currentClaim != null && SafeGet(seg, 1) == "QC":
                    // NM1-03 = LastName, NM1-04 = FirstName
                    var last  = SafeGet(seg, 3);
                    var first = SafeGet(seg, 4);
                    currentClaim.PatientName = string.IsNullOrEmpty(first) ? last : $"{first} {last}";
                    break;

                case "SVC" when currentClaim != null:
                    if (currentSvc != null) currentClaim.ServiceLines.Add(currentSvc);
                    var composite = SafeGet(seg, 1);
                    var parts     = composite.Split(':');
                    var procCode  = parts.Length > 1 ? parts[1] : parts[0];
                    currentSvc = new X835ServiceLine
                    {
                        ProcedureCode = procCode,
                        BilledAmount  = ParseDecimal(SafeGet(seg, 2)),
                        PaidAmount    = ParseDecimal(SafeGet(seg, 3)),
                    };
                    break;

                case "CAS" when currentSvc != null:
                    // Triplets: CAS01/02/03, CAS04/05/06, ...
                    var elems = seg.Elements;
                    for (int i = 0; i + 2 < elems.Count; i += 3)
                    {
                        var gc  = elems[i];
                        var rc  = elems[i + 1];
                        var amt = elems[i + 2];
                        if (string.IsNullOrEmpty(gc)) break;
                        currentSvc.Adjustments.Add(new X835Adjustment
                        {
                            GroupCode        = gc,
                            ReasonCode       = rc,
                            AdjustmentAmount = ParseDecimal(amt),
                        });
                    }
                    break;

                case "MOA" when currentClaim != null:
                    // MOA09 is index 9 (1-based)
                    var moa09 = SafeGet(seg, 9);
                    if (!string.IsNullOrEmpty(moa09))
                        currentClaim.RarcRemarkCode = moa09;
                    break;
            }
        }

        // Flush last claim
        if (currentClaim != null)
        {
            if (currentSvc != null) currentClaim.ServiceLines.Add(currentSvc);
            claims.Add(currentClaim);
        }
    }

    private static string SafeGet(X12Segment seg, int index)
    {
        var els = seg.Elements;
        return index <= els.Count ? els[index - 1] : "";
    }

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
