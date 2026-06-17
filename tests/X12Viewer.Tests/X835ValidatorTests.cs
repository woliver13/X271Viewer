using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X835ValidatorTests
{
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "tests835.edi");

    [Fact]
    public void Validate_WellFormed835_ReturnsNoErrors()
    {
        var doc = new X835DocumentParser().ParseFile(FixturePath);

        var result = new X835Validator().Validate(doc);
        Assert.Empty(result);
    }
}
