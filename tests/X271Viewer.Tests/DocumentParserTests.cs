using X271Viewer.Domain;

namespace X271Viewer.Tests;

public class DocumentParserTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void Parse_non_x12_content_throws_X271ParseException()
    {
        var parser = new X271DocumentParser();
        Assert.Throws<X271ParseException>(() => parser.ParseContent("this is not X12 at all"));
    }

    [Fact]
    public void Parse_file_not_found_throws_X271ParseException()
    {
        var parser = new X271DocumentParser();
        Assert.Throws<X271ParseException>(() => parser.ParseFile(@"C:\does\not\exist.edi"));
    }

    [Fact]
    public void Parse_valid271_returns_document_with_isa_raw_text()
    {
        var path = Path.Combine(FixtureDir, "sample271.edi");
        var parser = new X271DocumentParser();

        var doc = parser.ParseFile(path);

        Assert.NotNull(doc);
        Assert.Contains("ISA", doc.IsaRawText);
    }
}
