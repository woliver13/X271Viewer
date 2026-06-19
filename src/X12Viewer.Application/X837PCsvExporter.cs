using System.Text;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class X837PCsvExporter
{
    private static readonly string[] Headers =
    [
        "BillingProviderNPI", "BillingProviderName", "SubscriberID", "PatientName",
        "ClaimID", "PlaceOfService", "DiagnosisCodes",
        "ProcedureCode", "Modifier", "Units", "BilledAmount"
    ];

    public static string Export(X837PDocument doc)
    {
        var enriched = X837PInterpreter.Interpret(doc);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers));

        foreach (var claim in enriched.Claims)
        {
            var dxCodes   = string.Join(";", claim.DiagnosisCodes.Select(d => d.Code));
            var patName   = claim.Patient?.PatientName ?? claim.Subscriber.SubscriberName;

            if (claim.ServiceLines.Count == 0)
            {
                sb.AppendLine(BuildRow(enriched, claim, null, dxCodes, patName));
            }
            else
            {
                foreach (var svc in claim.ServiceLines)
                    sb.AppendLine(BuildRow(enriched, claim, svc, dxCodes, patName));
            }
        }

        return sb.ToString();
    }

    private static string BuildRow(X837PDocument doc, X837Claim claim, X837PServiceLine? svc, string dxCodes, string patName)
    {
        return string.Join(",",
        [
            Q(doc.BillingProvider.Npi),
            Q(doc.BillingProvider.Name),
            Q(claim.Subscriber.SubscriberId),
            Q(patName),
            Q(claim.ClaimId),
            Q(claim.PlaceOfService),
            Q(dxCodes),
            Q(svc?.ProcedureCode ?? ""),
            Q(svc?.Modifier      ?? ""),
            (svc?.Units         ?? 0).ToString("F2"),
            (svc?.BilledAmount  ?? 0).ToString("F2"),
        ]);
    }

    private static string Q(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
