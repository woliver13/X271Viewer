using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X837PTreeBuilderTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private X271Node BuildFromFixture()
    {
        var raw      = new X837PDocumentParser().ParseFile(Path.Combine(FixtureDir, "tests837p.edi"));
        var enriched = X837PInterpreter.Interpret(raw);
        return X837PTreeBuilder.Build(enriched);
    }

    // Criterion: X837PTreeBuilder.Build produces billing provider → subscriber → claim hierarchy
    [Fact]
    public void Build_ProducesBillingProviderRoot_WithSubscriberChildren()
    {
        var root = BuildFromFixture();
        Assert.Contains("METRO CLINIC", root.Label);
        Assert.Equal(2, root.Children.Count); // 2 subscribers
        Assert.All(root.Children, c => Assert.Contains("Subscriber", c.Label));
    }

    // Criterion: Patient node appears as child of subscriber when dependent present
    [Fact]
    public void Build_PatientNode_AppearsAsChildOfSubscriber_WhenDependentPresent()
    {
        var root = BuildFromFixture();
        // Subscriber 1 (JANE DOE) has claim 1 with a dependent patient (BABY DOE)
        var sub1 = root.Children[0];
        var patientNode = sub1.Children.FirstOrDefault(c => c.Label.Contains("Patient"));
        Assert.NotNull(patientNode);
        Assert.Contains("BABY DOE", patientNode!.Label);
    }

    // Criterion: Claim node label contains ClaimId and total billed amount
    [Fact]
    public void Build_ClaimNodeLabel_ContainsClaimIdAndBilledAmount()
    {
        var root = BuildFromFixture();
        // Sub 2 (JOHN SMITH) has CLAIM002 as a direct child
        var sub2 = root.Children[1];
        var claimNode = sub2.Children.FirstOrDefault(c => c.Label.Contains("CLAIM002"));
        Assert.NotNull(claimNode);
        Assert.Contains("50.00", claimNode!.Label);
    }

    // Criterion: Diagnosis node label contains ICD-10 code and description
    [Fact]
    public void Build_DiagnosisNode_ContainsCodeAndDescription()
    {
        var root = BuildFromFixture();
        var sub2      = root.Children[1];
        var claimNode = sub2.Children.First(c => c.Label.Contains("CLAIM002"));
        var dxNode    = claimNode.Children.FirstOrDefault(c => c.Label.Contains("Diagnosis"));
        Assert.NotNull(dxNode);
        Assert.Contains("Z00.00", dxNode!.Label);
    }

    // Criterion: SV1 service line node label contains procedure code and billed amount
    [Fact]
    public void Build_ServiceLineNode_ContainsProcedureCodeAndBilledAmount()
    {
        var root = BuildFromFixture();
        var sub2      = root.Children[1];
        var claimNode = sub2.Children.First(c => c.Label.Contains("CLAIM002"));
        var svcNode   = claimNode.Children.FirstOrDefault(c => c.Label.Contains("SV1"));
        Assert.NotNull(svcNode);
        Assert.Contains("99201", svcNode!.Label);
        Assert.Contains("50.00", svcNode.Label);
    }
}
