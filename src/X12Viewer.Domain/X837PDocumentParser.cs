using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Viewer.Domain;

public sealed class X837PDocumentParser
{
    public X837PDocument ParseFile(string filePath)
    {
        string content;
        try { content = File.ReadAllText(filePath); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
        {
            throw new X271ParseException($"Cannot read file: {filePath}", ex);
        }
        return ParseContent(content);
    }

    public X837PDocument ParseContent(string content)
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

        return Build(segments);
    }

    private static X837PDocument Build(List<X12Segment> segments)
    {
        var billingProvider = new X837BillingProvider();
        var subscriber      = new X837Subscriber();
        X837Patient? patient = null;
        var claims = new List<X837Claim>();

        string currentHlLevel = "";
        X837Claim? currentClaim = null;

        foreach (var seg in segments)
        {
            switch (seg.SegmentId)
            {
                case "HL":
                    currentHlLevel = SafeGet(seg, 3);
                    break;

                case "NM1" when SafeGet(seg, 1) == "85":
                    billingProvider = new X837BillingProvider
                    {
                        Npi  = SafeGet(seg, 9),
                        Name = SafeGet(seg, 3),
                    };
                    break;

                case "NM1" when currentHlLevel == "22" && SafeGet(seg, 1) == "IL":
                {
                    var last  = SafeGet(seg, 3);
                    var first = SafeGet(seg, 4);
                    subscriber = new X837Subscriber
                    {
                        SubscriberId   = SafeGet(seg, 9),
                        SubscriberName = string.IsNullOrEmpty(first) ? last : $"{first} {last}",
                    };
                    patient = null;
                    break;
                }

                case "NM1" when currentHlLevel == "23" && SafeGet(seg, 1) == "QC":
                {
                    var last  = SafeGet(seg, 3);
                    var first = SafeGet(seg, 4);
                    patient = new X837Patient
                    {
                        PatientName = string.IsNullOrEmpty(first) ? last : $"{first} {last}",
                    };
                    break;
                }

                case "CLM":
                {
                    if (currentClaim is not null)
                        claims.Add(currentClaim);

                    var posComposite = SafeGet(seg, 5);
                    var pos = posComposite.Contains(':') ? posComposite.Split(':')[0] : posComposite;

                    currentClaim = new X837Claim
                    {
                        ClaimId        = SafeGet(seg, 1),
                        BilledAmount   = ParseDecimal(SafeGet(seg, 2)),
                        PlaceOfService = pos,
                        Subscriber     = subscriber,
                        Patient        = patient,
                    };
                    break;
                }

                case "HI" when currentClaim is not null:
                {
                    var composite = SafeGet(seg, 1);
                    var code = composite.Contains(':') ? composite.Split(':')[1] : composite;
                    if (!string.IsNullOrEmpty(code))
                        currentClaim.DiagnosisCodes.Add(new X837DiagnosisCode(code));
                    break;
                }

                case "SV1" when currentClaim is not null:
                {
                    var procComposite = SafeGet(seg, 1);
                    var parts = procComposite.Split(':');
                    var procCode = parts.Length > 1 ? parts[1] : parts[0];
                    var modifier = parts.Length > 2 ? parts[2] : "";

                    currentClaim.ServiceLines.Add(new X837PServiceLine
                    {
                        ProcedureCode = procCode,
                        Modifier      = modifier,
                        BilledAmount  = ParseDecimal(SafeGet(seg, 2)),
                        Units         = ParseDecimal(SafeGet(seg, 4)),
                    });
                    break;
                }
            }
        }

        if (currentClaim is not null)
            claims.Add(currentClaim);

        return new X837PDocument
        {
            BillingProvider = billingProvider,
            Claims          = claims,
        };
    }

    private static string SafeGet(X12Segment seg, int index)
    {
        var els = seg.Elements;
        return index <= els.Count ? els[index - 1] : "";
    }

    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value, out var d) ? d : 0m;
}
