using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X277InterpreterTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [Fact]
    public void Interpret_Resolves_F1_Category_To_Finalized()
    {
        var doc = new X277Document();
        doc.Claims.Add(new X277Claim { ClaimId = "CLM-001", StatusCategoryCode = "F1", StatusCode = "20" });

        var result = X277Interpreter.Interpret(doc);

        Assert.Equal("Finalized", result.Claims[0].StatusDescription);
    }

    [Theory]
    [InlineData("F1", "accepted scenario")]
    [InlineData("A2", "pending scenario")]
    [InlineData("F2", "rejected scenario")]
    [InlineData("A7", "additional-info scenario")]
    public void Interpret_AllFourScenarios_HaveNonEmpty_StatusDescription(string categoryCode, string _)
    {
        var doc = new X277Document();
        doc.Claims.Add(new X277Claim { StatusCategoryCode = categoryCode, StatusCode = "20" });

        var result = X277Interpreter.Interpret(doc);

        Assert.NotEmpty(result.Claims[0].StatusDescription!);
    }
}
