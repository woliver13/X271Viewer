using System.Text.Json;
using woliver13.X271Viewer.Domain;

namespace woliver13.X271Viewer.Application;

public static class CliRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        var command  = args.Length > 0 ? args[0].ToLowerInvariant() : "--help";
        var filePath = args.Length > 1 ? args[1] : null;

        return command switch
        {
            "parse"             => RunParse(filePath, stdout, stderr),
            "interpret"         => RunInterpret(filePath, stdout, stderr),
            "validate"          => RunValidate(filePath, stdout, stderr),
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

        var validator = new X271ValidationService();
        var result    = validator.Validate(content);
        var errors    = result.Errors.Select(e => new { code = e.Code, message = e.Message }).ToList();
        stdout.Write(JsonSerializer.Serialize(new { isValid = result.IsValid, errors }, JsonOptions));
        return 0;
    }

    private static int RunHelp(TextWriter stdout)
    {
        stdout.WriteLine("Usage: x271 <command> [file]");
        stdout.WriteLine();
        stdout.WriteLine("Commands:");
        stdout.WriteLine("  parse      <file>   Output raw segment tree as JSON");
        stdout.WriteLine("  interpret  <file>   Output interpreted node tree as JSON (Phase 5 schema)");
        stdout.WriteLine("  validate   <file>   Output validation results as JSON");
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
