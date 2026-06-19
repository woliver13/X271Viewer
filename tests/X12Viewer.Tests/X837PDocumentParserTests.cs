using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X837PDocumentParserTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private X837PDocument ParseFixture() =>
        new X837PDocumentParser().ParseFile(FixturePath("tests837p.edi"));

    [Fact]
    public void ParseFile_ReturnsCorrectClaimCount()
    {
        var doc = ParseFixture();
        Assert.Equal(2, doc.Claims.Count);
    }
}
