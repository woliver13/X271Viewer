using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Viewer.Domain;

public sealed class X270DocumentParser
{
    public X270Document ParseFile(string filePath)
    {
        string content;
        try { content = File.ReadAllText(filePath); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
        {
            throw new X271ParseException($"Cannot read file: {filePath}", ex);
        }
        return ParseContent(content);
    }

    public X270Document ParseContent(string content)
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

    private static X270Document Build(List<X12Segment> segments)
    {
        var doc = new X270Document();

        string currentHlLevel = "";
        string? subsId = null, subsName = null;
        var subsEqs = new List<X270ServiceTypeQuery>();
        string? depName = null;
        var depEqs = new List<X270ServiceTypeQuery>();

        foreach (var seg in segments)
        {
            if (seg.SegmentId == "HL")
            {
                var hlLevel = SafeGet(seg, 3);

                if (hlLevel == "22")
                {
                    // Flush any pending dependent onto last subscriber
                    if (depName != null && doc.Subscribers.Count > 0)
                    {
                        var dep = new X270Dependent(depName, depEqs.ToList());
                        var last = doc.Subscribers[^1];
                        doc.Subscribers[^1] = last with { Dependent = dep };
                        depName = null;
                        depEqs.Clear();
                    }

                    // Flush pending subscriber
                    if (subsId != null || subsName != null)
                    {
                        doc.Subscribers.Add(new X270Subscriber(subsId ?? "", subsName ?? "", subsEqs.ToList()));
                        subsId = null;
                        subsName = null;
                        subsEqs.Clear();
                    }

                    currentHlLevel = "22";
                }
                else if (hlLevel == "23")
                {
                    // Flush pending subscriber before entering dependent
                    if (subsId != null || subsName != null)
                    {
                        doc.Subscribers.Add(new X270Subscriber(subsId ?? "", subsName ?? "", subsEqs.ToList()));
                        subsId = null;
                        subsName = null;
                        subsEqs.Clear();
                    }

                    currentHlLevel = "23";
                }
                else
                {
                    currentHlLevel = hlLevel;
                }
            }
            else if (seg.SegmentId == "NM1" && currentHlLevel == "22" && SafeGet(seg, 1) == "IL")
            {
                var last = SafeGet(seg, 3);
                var first = SafeGet(seg, 4);
                subsName = string.IsNullOrEmpty(first) ? last : $"{first} {last}";
                subsId = SafeGet(seg, 9);
            }
            else if (seg.SegmentId == "NM1" && currentHlLevel == "23" && SafeGet(seg, 1) == "03")
            {
                var last = SafeGet(seg, 3);
                var first = SafeGet(seg, 4);
                depName = string.IsNullOrEmpty(first) ? last : $"{first} {last}";
            }
            else if (seg.SegmentId == "EQ" && currentHlLevel == "22")
            {
                subsEqs.Add(new X270ServiceTypeQuery(SafeGet(seg, 1)));
            }
            else if (seg.SegmentId == "EQ" && currentHlLevel == "23")
            {
                depEqs.Add(new X270ServiceTypeQuery(SafeGet(seg, 1)));
            }
        }

        // Flush remaining dependent
        if (depName != null && doc.Subscribers.Count > 0)
        {
            var dep = new X270Dependent(depName, depEqs.ToList());
            var last = doc.Subscribers[^1];
            doc.Subscribers[^1] = last with { Dependent = dep };
        }

        // Flush remaining subscriber
        if (subsId != null || subsName != null)
        {
            doc.Subscribers.Add(new X270Subscriber(subsId ?? "", subsName ?? "", subsEqs.ToList()));
        }

        return doc;
    }

    private static string SafeGet(X12Segment seg, int index)
    {
        var els = seg.Elements;
        return index <= els.Count ? els[index - 1] : "";
    }
}
