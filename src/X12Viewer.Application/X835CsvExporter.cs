using System.Text;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X835CsvExporter
{
    private static readonly string[] Headers =
    [
        "Payer", "ClaimId", "PatientName", "TotalBilled", "TotalPaid", "ClaimStatus",
        "ProcedureCode", "SvcBilled", "SvcPaid", "CARCCode", "CARCDescription", "AdjustmentAmount"
    ];

    public static string Export(X835Document doc)
    {
        var enriched = X835Interpreter.Interpret(doc);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers));
        foreach (var claim in enriched.Claims)
        {
            if (claim.ServiceLines.Count == 0)
            {
                sb.AppendLine(BuildRow(claim, null, null));
            }
            else
            {
                foreach (var svc in claim.ServiceLines)
                    sb.AppendLine(BuildRow(claim, svc, svc.Adjustments.FirstOrDefault()));
            }
        }
        return sb.ToString();
    }

    private static string BuildRow(X835Claim claim, X835ServiceLine? svc, X835Adjustment? adj)
    {
        return string.Join(",",
        [
            Q(""),
            Q(claim.ClaimId),
            Q(claim.PatientName),
            claim.BilledAmount.ToString("F2"),
            claim.PaidAmount.ToString("F2"),
            Q(claim.ClaimStatusCode),
            Q(svc?.ProcedureCode ?? ""),
            (svc?.BilledAmount ?? 0).ToString("F2"),
            (svc?.PaidAmount ?? 0).ToString("F2"),
            Q(adj?.ReasonCode ?? ""),
            Q(adj?.ReasonDescription ?? ""),
            adj is null ? "" : adj.AdjustmentAmount.ToString("F2"),
        ]);
    }

    private static string Q(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
