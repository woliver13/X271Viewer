using System.Text.Json;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Application;

public static class CliRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions JsonCamelOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        var command  = args.Length > 0 ? args[0].ToLowerInvariant() : "--help";
        var filePath = args.Length > 1 ? args[1] : null;

        return command switch
        {
            "parse"             => RunParse(filePath, stdout, stderr),
            "interpret"         => RunInterpret(filePath, stdout, stderr),
            "validate"          => RunValidate(filePath, stdout, stderr),
            "export"            => RunExport(filePath, stdout, stderr),
            "view"              => RunHelp(stdout), // view handled by Program.cs; show help in tests
            "--help" or "help"  => RunHelp(stdout),
            _                   => RunHelp(stdout),
        };
    }

    private static int RunParse(string? filePath, TextWriter stdout, TextWriter stderr)
    {
        var content = ReadFile(filePath, stderr);
        if (content is null) return 1;

        var doc = ParseDoc(content, stderr);
        if (doc is null) return 2;

        var st = doc.Segments.FirstOrDefault(s => s.SegmentId == "ST");
        if (st is not null && st.Elements.Count > 0 && st.Elements[0] == "835")
        {
            try
            {
                var doc835 = new X835DocumentParser().ParseContent(content);
                stdout.Write(JsonSerializer.Serialize(doc835, JsonCamelOptions));
                return 0;
            }
            catch (X271ParseException ex)
            {
                stderr.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }

        var segments = doc.Segments.Select(s => X271TreeBuilder.SegmentToRaw(s, doc.Delimiters)).ToList();
        stdout.Write(JsonSerializer.Serialize(new { segments }, JsonOptions));
        return 0;
    }

    private static int RunInterpret(string? filePath, TextWriter stdout, TextWriter stderr)
    {
        var content = ReadFile(filePath, stderr);
        if (content is null) return 1;

        var doc = ParseDoc(content, stderr);
        if (doc is null) return 2;

        var st = doc.Segments.FirstOrDefault(s => s.SegmentId == "ST");
        if (st is not null && st.Elements.Count > 0 && st.Elements[0] == "835")
        {
            try
            {
                var doc835 = new X835DocumentParser().ParseContent(content);
                var enriched = X835Interpreter.Interpret(doc835);
                stdout.Write(JsonSerializer.Serialize(enriched, JsonCamelOptions));
                return 0;
            }
            catch (X271ParseException ex)
            {
                stderr.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }

        var root      = X271TreeBuilder.Build(doc);
        var validator = new X271ValidationService();
        var valResult = validator.Validate(content);
        validator.AnnotateTree(root, valResult);

        var exportDoc = X271JsonExporter.BuildExportDocument(root, valResult, doc.IsaRawText);
        stdout.Write(X271JsonExporter.Export(exportDoc));
        return 0;
    }

    private static int RunValidate(string? filePath, TextWriter stdout, TextWriter stderr)
    {
        var content = ReadFile(filePath, stderr);
        if (content is null) return 1;

        var doc = ParseDoc(content, stderr);
        if (doc is null) return 2;

        var st = doc.Segments.FirstOrDefault(s => s.SegmentId == "ST");
        if (st is not null && st.Elements.Count > 0 && st.Elements[0] == "835")
        {
            try
            {
                var doc835  = new X835DocumentParser().ParseContent(content);
                var issues  = new X835Validator().Validate(doc835);
                if (issues.Count == 0)
                {
                    stdout.Write(JsonSerializer.Serialize(new { isValid = true, errors = Array.Empty<string>() }, JsonOptions));
                    return 0;
                }
                stderr.WriteLine($"Error: {issues.Count} validation issue(s) found.");
                foreach (var issue in issues)
                    stderr.WriteLine($"  - {issue}");
                return 1;
            }
            catch (X271ParseException ex)
            {
                stderr.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }

        var validator = new X271ValidationService();
        var result    = validator.Validate(content);
        var errors    = result.Errors.Select(e => new { code = e.Code, message = e.Message }).ToList();
        stdout.Write(JsonSerializer.Serialize(new { isValid = result.IsValid, errors }, JsonOptions));
        return 0;
    }

    private static int RunExport(string? filePath, TextWriter stdout, TextWriter stderr)
    {
        var content = ReadFile(filePath, stderr);
        if (content is null) return 1;

        var doc = ParseDoc(content, stderr);
        if (doc is null) return 2;

        var st = doc.Segments.FirstOrDefault(s => s.SegmentId == "ST");
        if (st is not null && st.Elements.Count > 0 && st.Elements[0] == "835")
        {
            try
            {
                var doc835 = new X835DocumentParser().ParseContent(content);
                var csv = X835CsvExporter.Export(doc835);
                stdout.Write(csv);
                return 0;
            }
            catch (X271ParseException ex)
            {
                stderr.WriteLine($"Error: {ex.Message}");
                return 2;
            }
        }

        stderr.WriteLine("Error: export is only supported for 835 files");
        return 1;
    }

    private static int RunHelp(TextWriter stdout)
    {
        stdout.WriteLine("Usage: x271 <command> [file]");
        stdout.WriteLine();
        stdout.WriteLine("Commands:");
        stdout.WriteLine("  parse      <file>   Output raw segment tree as JSON");
        stdout.WriteLine("  interpret  <file>   Output interpreted node tree as JSON (Phase 5 schema)");
        stdout.WriteLine("  validate   <file>   Output validation results as JSON");
        stdout.WriteLine("  export     <file>   Export 835 as CSV to stdout");
        stdout.WriteLine("  view       <file>   Launch the WPF viewer with the file pre-loaded");
        return 0;
    }

    private static string? ReadFile(string? filePath, TextWriter stderr)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            stderr.WriteLine($"Error: file not found: {filePath}");
            return null;
        }
        return File.ReadAllText(filePath);
    }

    private static X271Document? ParseDoc(string content, TextWriter stderr)
    {
        try
        {
            return new X271DocumentParser().ParseContent(content);
        }
        catch (X271ParseException ex)
        {
            stderr.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}
