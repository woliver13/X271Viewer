using woliver13.X12Net.Core;

namespace woliver13.X271Viewer.Domain;

public static class X271TreeBuilder
{
    public static X271Node Build(X271Document doc)
    {
        var d    = doc.Delimiters;
        var segs = doc.Segments;

        var isa    = segs.First(s => Id(s) == "ISA");
        var isaRaw = Raw(isa, d);

        var gsNodes = BuildGsNodes(segs, d);

        return new X271Node($"ISA — Interchange {isa[13]}", [isaRaw], gsNodes,
            isCollapsedByDefault: true);
    }

    // ── GS level ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<X271Node> BuildGsNodes(
        IReadOnlyList<X12Segment> segs, X12Delimiters d)
    {
        var result = new List<X271Node>();
        int i = 0;
        while (i < segs.Count)
        {
            if (Id(segs[i]) != "GS") { i++; continue; }

            int gsIdx = i++;
            var stNodes = new List<X271Node>();

            while (i < segs.Count && Id(segs[i]) != "GS" && Id(segs[i]) != "IEA")
            {
                if (Id(segs[i]) == "ST")
                    stNodes.Add(BuildStNode(segs, ref i, d));
                else
                    i++;
            }

            var gs    = segs[gsIdx];
            var label = $"GS — Functional Group {gs[1]} ({gs[8]})";
            result.Add(new X271Node(label, [Raw(gs, d)], stNodes, isCollapsedByDefault: true));
        }
        return result;
    }

    // ── ST level ─────────────────────────────────────────────────────────────

    private static X271Node BuildStNode(
        IReadOnlyList<X12Segment> segs, ref int i, X12Delimiters d)
    {
        int stIdx = i++;

        var preHlRaw  = new List<string> { Raw(segs[stIdx], d) };
        var hlSegments = new List<(int idx, X12Segment seg)>();

        while (i < segs.Count && Id(segs[i]) != "SE" && Id(segs[i]) != "GE")
        {
            if (Id(segs[i]) == "HL")
                hlSegments.Add((i, segs[i]));
            else if (hlSegments.Count == 0)
                preHlRaw.Add(Raw(segs[i], d));
            i++;
        }
        if (i < segs.Count && Id(segs[i]) == "SE") i++; // consume SE

        var st    = segs[stIdx];
        var label = $"ST — Transaction {st[1]} ({st[2]})";

        var hlNodes = BuildHlTree(hlSegments, segs, d);

        return new X271Node(label, preHlRaw, hlNodes);
    }

    // ── HL level ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<X271Node> BuildHlTree(
        List<(int idx, X12Segment seg)> hls,
        IReadOnlyList<X12Segment> segs,
        X12Delimiters d)
    {
        // Build a lookup: hlId → node placeholder
        var nodes = new Dictionary<string, X271Node>();
        var childMap = new Dictionary<string, List<X271Node>>();
        var roots = new List<X271Node>();

        foreach (var (hlIdx, hlSeg) in hls)
        {
            string hlId       = hlSeg[1];
            string parentId   = hlSeg[2]; // HL02 — parent HL id (empty for root)
            string levelCode  = hlSeg[3]; // HL03 — 20=Source,21=Receiver,22=Sub,23=Dep

            var associatedRaw = new List<string> { Raw(hlSeg, d) };

            // Collect segments belonging to this HL (up to next HL or SE)
            int j = hlIdx + 1;
            while (j < segs.Count && Id(segs[j]) != "HL" && Id(segs[j]) != "SE"
                                   && Id(segs[j]) != "GE")
            {
                if (Id(segs[j]) != "EB")
                    associatedRaw.Add(Raw(segs[j], d));
                j++;
            }

            // Collect EB segments for this HL
            var ebGroups = BuildEbGroups(hlIdx, segs, d);

            var hlNode = new X271Node(HlLabel(levelCode, hlSeg), associatedRaw, ebGroups);
            nodes[hlId] = hlNode;
            childMap[hlId] = new List<X271Node>(ebGroups);

            if (string.IsNullOrEmpty(parentId))
                roots.Add(hlNode);
            else if (childMap.TryGetValue(parentId, out var siblings))
                siblings.Add(hlNode);
            else
                roots.Add(hlNode);
        }

        // Rebuild nodes with their accumulated children (EB groups + child HLs)
        return RebuildWithChildren(hls, nodes, childMap, roots, segs, d);
    }

    private static IReadOnlyList<X271Node> RebuildWithChildren(
        List<(int idx, X12Segment seg)> hls,
        Dictionary<string, X271Node> nodes,
        Dictionary<string, List<X271Node>> childMap,
        List<X271Node> roots,
        IReadOnlyList<X12Segment> segs,
        X12Delimiters d)
    {
        // Since X271Node is immutable, rebuild from leaves up
        var rebuilt = new Dictionary<string, X271Node>();

        // Process in reverse so children are built before parents
        for (int k = hls.Count - 1; k >= 0; k--)
        {
            var (hlIdx, hlSeg) = hls[k];
            string hlId      = hlSeg[1];
            string levelCode = hlSeg[3];

            var associatedRaw = new List<string> { Raw(hlSeg, d) };
            int j = hlIdx + 1;
            while (j < segs.Count && Id(segs[j]) != "HL" && Id(segs[j]) != "SE"
                                   && Id(segs[j]) != "GE")
            {
                if (Id(segs[j]) != "EB")
                    associatedRaw.Add(Raw(segs[j], d));
                j++;
            }

            var ebGroups = BuildEbGroups(hlIdx, segs, d);

            // Gather already-rebuilt HL children for this node
            var children = new List<X271Node>(ebGroups);
            foreach (var (_, childSeg) in hls)
            {
                if (childSeg[2] == hlId && rebuilt.TryGetValue(childSeg[1], out var childNode))
                    children.Add(childNode);
            }

            rebuilt[hlId] = new X271Node(HlLabel(levelCode, hlSeg), associatedRaw, children);
        }

        // Return root HL nodes (no parent id)
        return hls
            .Where(h => string.IsNullOrEmpty(h.seg[2]))
            .Select(h => rebuilt[h.seg[1]])
            .ToList();
    }

    // ── EB grouping ──────────────────────────────────────────────────────────

    private static IReadOnlyList<X271Node> BuildEbGroups(
        int hlIdx, IReadOnlyList<X12Segment> segs, X12Delimiters d)
    {
        // Collect (EB segment index, EB segment) pairs owned by this HL
        var ebEntries = new List<(int idx, X12Segment seg)>();
        for (int j = hlIdx + 1; j < segs.Count; j++)
        {
            var sid = Id(segs[j]);
            if (sid == "HL" || sid == "SE" || sid == "GE") break;
            if (sid == "EB") ebEntries.Add((j, segs[j]));
        }

        if (ebEntries.Count == 0) return [];

        // Build leaf nodes: each EB + its companion segments up to the next EB/HL/SE/GE
        var leaves = ebEntries.Select((entry, i) =>
        {
            var (ebIdx, ebSeg) = entry;
            var raw = new List<string> { Raw(ebSeg, d) };

            int limit = i + 1 < ebEntries.Count
                ? ebEntries[i + 1].idx
                : segs.Count;

            for (int j = ebIdx + 1; j < limit && j < segs.Count; j++)
            {
                var sid = Id(segs[j]);
                if (sid == "HL" || sid == "SE" || sid == "GE") break;
                if (sid != "EB") raw.Add(Raw(segs[j], d));
            }

            return new X271Node($"EB {ebSeg[1]}/{ebSeg[2]}", raw, []);
        }).ToList();

        // Group leaves by EB01 (Service Type code)
        var grouped = leaves
            .GroupBy(n => n.RawSegments[0].Split('*')[1])
            .Select(g =>
            {
                var groupLabel = $"EB — Service Type {g.Key}";
                return new X271Node(groupLabel, [], g.ToList<X271Node>());
            })
            .ToList<X271Node>();

        return grouped;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HlLabel(string levelCode, X12Segment hlSeg) => levelCode switch
    {
        "20" => $"HL — Information Source ({hlSeg[1]})",
        "21" => $"HL — Information Receiver ({hlSeg[1]})",
        "22" => $"HL — Subscriber ({hlSeg[1]})",
        "23" => $"HL — Dependent ({hlSeg[1]})",
        _    => $"HL — Level {levelCode} ({hlSeg[1]})",
    };

    public static string SegmentToRaw(X12Segment seg, X12Delimiters d)
    {
        var elements = string.Join(d.ElementSeparator, seg.Elements);
        return $"{Id(seg)}{d.ElementSeparator}{elements}{d.SegmentTerminator}";
    }

    private static string Raw(X12Segment seg, X12Delimiters d) => SegmentToRaw(seg, d);

    // SegmentId may have leading/trailing whitespace when EDI lines end with \n after ~.
    private static string Id(X12Segment seg) => seg.SegmentId.Trim();
}
