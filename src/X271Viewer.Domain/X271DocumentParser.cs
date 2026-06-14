using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace X271Viewer.Domain;

public sealed class X271DocumentParser
{
    public X271Document ParseFile(string filePath)
    {
        string content;
        try { content = File.ReadAllText(filePath); }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
        {
            throw new X271ParseException($"Cannot read file: {filePath}", ex);
        }
        return ParseContent(content);
    }

    public X271Document ParseContent(string content)
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

        var isaRaw = FormatSegment(isa, content);
        var delimiters = X12Delimiters.FromIsa(content);

        return new X271Document
        {
            IsaRawText = isaRaw,
            Segments = segments,
            Delimiters = delimiters,
        };
    }

    private static string FormatSegment(X12Segment segment, string rawContent)
    {
        // Re-extract the ISA line from raw content (it's always the first 106 chars)
        var end = rawContent.IndexOf('~');
        return end >= 0 ? rawContent[..(end + 1)] : rawContent;
    }
}
