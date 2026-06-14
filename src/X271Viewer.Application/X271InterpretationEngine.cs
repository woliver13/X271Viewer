using System.Globalization;
using System.Text;
using X271Viewer.Domain;

namespace X271Viewer.Application;

public static class X271InterpretationEngine
{
    public static string Interpret(X271Node node)
    {
        var sb = new StringBuilder();

        foreach (var seg in node.RawSegments)
        {
            var elements = seg.TrimEnd('~').Split('*');
            var id = elements[0];

            switch (id)
            {
                case "EB":
                    AppendEb(sb, elements);
                    break;
                case "NM1":
                    AppendNm1(sb, elements);
                    break;
                case "DMG":
                    AppendDmg(sb, elements);
                    break;
            }
        }

        return sb.Length > 0
            ? sb.ToString().TrimEnd()
            : $"[{node.Label}] — no interpretation available";
    }

    public static string FormatAmount(string raw)
    {
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            return amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        return raw;
    }

    // ── private helpers ───────────────────────────────────────────────────────

    private static void AppendEb(StringBuilder sb, string[] e)
    {
        var benefitCode   = GetElement(e, 1);
        var coverageLevel = GetElement(e, 2);
        var serviceType   = GetElement(e, 3);
        var insuranceType = GetElement(e, 4);
        var amountStr     = GetElement(e, 5);   // EB05: Plan Coverage Description (used as amount in practice)
        var timePeriod    = GetElement(e, 6);   // EB06: Time Period Qualifier

        var benefitLabel   = X12CodeTable.Resolve("EB01", benefitCode);
        var coverageLabel  = X12CodeTable.Resolve("EB02", coverageLevel);
        var serviceLabel   = X12CodeTable.Resolve("EB03", serviceType);
        var insurLabel     = string.IsNullOrEmpty(insuranceType) ? "" : X12CodeTable.Resolve("EB05", insuranceType);
        var timePeriodLabel = string.IsNullOrEmpty(timePeriod)   ? "" : X12CodeTable.Resolve("EB06", timePeriod);

        sb.AppendLine($"Benefit: {benefitLabel}");
        sb.AppendLine($"  Service Type:   {serviceLabel}");
        sb.AppendLine($"  Coverage Level: {coverageLabel}");

        if (!string.IsNullOrEmpty(insurLabel))
            sb.AppendLine($"  Insurance Type: {insurLabel}");

        if (!string.IsNullOrEmpty(amountStr) &&
            decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            sb.AppendLine($"  Amount:         {FormatAmount(amountStr)}");

        if (!string.IsNullOrEmpty(timePeriodLabel))
            sb.AppendLine($"  Time Period:    {timePeriodLabel}");

        sb.AppendLine();
    }

    private static void AppendNm1(StringBuilder sb, string[] e)
    {
        var qualifierCode = GetElement(e, 1);
        var lastName      = GetElement(e, 3);
        var firstName     = GetElement(e, 4);
        var middleName    = GetElement(e, 5);
        var idCode        = GetElement(e, 9);

        var qualifier = qualifierCode switch
        {
            "IL" => "Subscriber",
            "QC" => "Dependent",
            "PR" => "Payer",
            "1P" => "Provider",
            _    => qualifierCode
        };

        var name = string.IsNullOrEmpty(firstName)
            ? lastName
            : $"{firstName}{(string.IsNullOrEmpty(middleName) ? "" : " " + middleName)} {lastName}".Trim();

        sb.AppendLine($"{qualifier}: {name}");

        if (!string.IsNullOrEmpty(idCode))
            sb.AppendLine($"  Member ID: {idCode}");
    }

    private static void AppendDmg(StringBuilder sb, string[] e)
    {
        var dateQualifier = GetElement(e, 1);
        var dateValue     = GetElement(e, 2);
        var genderCode    = GetElement(e, 3);

        if (dateQualifier == "D8" && dateValue.Length == 8 &&
            DateTime.TryParseExact(dateValue, "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
        {
            sb.AppendLine($"  Date of Birth: {dob:MMMM d, yyyy}");
        }

        var gender = genderCode switch
        {
            "M" => "Male",
            "F" => "Female",
            "U" => "Unknown",
            _   => genderCode
        };

        if (!string.IsNullOrEmpty(gender))
            sb.AppendLine($"  Gender: {gender}");
    }

    private static string GetElement(string[] elements, int index) =>
        index < elements.Length ? elements[index] : string.Empty;
}
