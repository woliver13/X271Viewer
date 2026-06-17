using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X277InterpreterTests
{
    [Fact]
    public void Interpret_Resolves_F1_Category_To_Finalized()
    {
        var doc = new X277Document();
        doc.Claims.Add(new X277Claim { ClaimId = "CLM-001", StatusCategoryCode = "F1", StatusCode = "20" });

        var result = X277Interpreter.Interpret(doc);

        Assert.Equal("Finalized", result.Claims[0].StatusDescription);
    }
}
