using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X277DocumentParserTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private readonly X277DocumentParser _parser = new();

    [Fact]
    public void ParseFile_ReturnsCorrectClaimCount()
    {
        var doc = _parser.ParseFile(FixturePath("tests277.edi"));
        Assert.Equal(2, doc.Claims.Count);
    }

    [Fact]
    public void ParseFile_ReturnsClaimId_And_StatusCodes()
    {
        var doc = _parser.ParseFile(FixturePath("tests277.edi"));
        var first = doc.Claims[0];
        Assert.Equal("CLM-001", first.ClaimId);
        Assert.Equal("F1", first.StatusCategoryCode);
        Assert.Equal("20", first.StatusCode);
    }
}
