using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X835CsvExporterTests
{
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "tests835.edi");

    // ── Criterion 5 ──────────────────────────────────────────────────────────
    [Fact]
    public void Cli_Export_835_WritesToStdoutWithCorrectHeaderAndRowCount()
    {
        var stdout = new System.IO.StringWriter();
        var stderr = new System.IO.StringWriter();

        var exitCode = CliRunner.Run(["export", FixturePath], stdout, stderr);

        Assert.Equal(0, exitCode);
        var csv  = stdout.ToString();
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First row is the header
        Assert.StartsWith("Payer,ClaimId,", rows[0]);
        // 4 claims × 1 SVC = 4 data rows + 1 header
        Assert.Equal(5, rows.Length);
    }

    // ── Criterion 4 ──────────────────────────────────────────────────────────
    [Fact]
    public void Export_ClaimWithNoSvcLines_ProducesOneRowWithZeroSvcBilled()
    {
        // Minimal 835 with one CLP but NO SVC segment
        const string edi =
            "ISA*00*          *00*          *ZZ*PAYER          *ZZ*PROVIDER       *260617*0900*^*00501*000000001*0*P*:~" +
            "GS*HP*PAYER*PROVIDER*20260617*0900*1*X*005010X221A1~" +
            "ST*835*0001~" +
            "BPR*I*0.00*C*ACH*CTX*01*999999999*DA*12345*1234567890**01*888888888*DA*98765*20260617~" +
            "TRN*1*835NOSVC*1234567890~" +
            "CLP*CLM-NOSVC*4*500.00*0.00**HC*99213*1~" +
            "NM1*QC*1*NOBODY*TEST****MI*ZZZ999~" +
            "SE*7*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var doc  = new X835DocumentParser().ParseContent(edi);
        var csv  = X835CsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 1 header + 1 data row (no SVC → single claim-level row)
        Assert.Equal(2, rows.Length);

        var cols = SplitCsvRow(rows[1]);
        Assert.Equal(12, cols.Count);
        Assert.Equal("0.00", cols[7]);  // SvcBilled
        Assert.Equal("0.00", cols[8]);  // SvcPaid
        Assert.Equal("", cols[6]);      // ProcedureCode
    }

    // ── Criterion 3 ──────────────────────────────────────────────────────────
    [Fact]
    public void Export_CarcDescriptionNonEmpty_ForClaimsWithAdjustments()
    {
        var doc  = new X835DocumentParser().ParseFile(FixturePath);
        var csv  = X835CsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // CLM-002, CLM-003, CLM-004 all have CAS CO adjustments; their rows must have a non-empty
        // CARCDescription column (column index 10, 0-based after splitting on comma — but fields may
        // be quoted; easiest: check that the row does NOT end with ",," meaning empty desc).
        // The fixture has CARC 45 and 50 — both are known CARC codes with descriptions.
        var dataRows = rows.Skip(1).ToList();
        // Rows with an adjustment: CLM-002 (row index 1), CLM-003 (row index 2), CLM-004 (row index 3)
        foreach (var row in dataRows.Where(r => r.Contains("CLM-002") || r.Contains("CLM-003") || r.Contains("CLM-004")))
        {
            var cols = SplitCsvRow(row);
            Assert.Equal(12, cols.Count);
            Assert.False(string.IsNullOrWhiteSpace(cols[10]),
                $"CARCDescription (col 10) is empty in row: {row}");
        }
    }

    private static List<string> SplitCsvRow(string row)
    {
        // Simple CSV split that handles quoted fields
        var result = new List<string>();
        bool inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in row)
        {
            if (c == '"') { inQuote = !inQuote; }
            else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result;
    }

    // ── Criterion 2 ──────────────────────────────────────────────────────────
    [Fact]
    public void Export_DataRowsContainClaimIdAndPatientName()
    {
        var doc  = new X835DocumentParser().ParseFile(FixturePath);
        var csv  = X835CsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header row; every data row must contain its claim's ClaimId and PatientName
        foreach (var row in rows.Skip(1))
        {
            Assert.Matches(@"CLM-00[1-4]", row);   // ClaimId present
            Assert.True(
                row.Contains("DOE") || row.Contains("SMITH") ||
                row.Contains("JONES") || row.Contains("BROWN"),
                $"PatientName not found in row: {row}");
        }
    }

    // ── Criterion 1 ──────────────────────────────────────────────────────────
    [Fact]
    public void Export_ProducesCorrectRowCount()
    {
        var doc  = new X835DocumentParser().ParseFile(FixturePath);
        var csv  = X835CsvExporter.Export(doc);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 4 claims × 1 SVC each = 4 data rows + 1 header
        Assert.Equal(5, rows.Length);
    }
}
