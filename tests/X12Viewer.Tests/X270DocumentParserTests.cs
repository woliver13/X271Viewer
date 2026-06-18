using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X270DocumentParserTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    private X270Document ParseFixture() =>
        new X270DocumentParser().ParseFile(FixturePath("tests270.edi"));

    // Cycle 1
    [Fact]
    public void X270DocumentParser_ParseFile_returns_correct_subscriber_count()
    {
        var doc = ParseFixture();
        Assert.Equal(2, doc.Subscribers.Count);
    }

    // Cycle 3: dependent patient name
    [Fact]
    public void X270Subscriber_dependent_populates_PatientName_when_present()
    {
        var doc = ParseFixture();
        var sub = doc.Subscribers[1]; // JOHN SMITH has dependent BABY SMITH
        Assert.NotNull(sub.Dependent);
        Assert.Equal("BABY SMITH", sub.Dependent!.PatientName);
    }

    [Fact]
    public void X270Subscriber_without_dependent_has_null_Dependent()
    {
        var doc = ParseFixture();
        Assert.Null(doc.Subscribers[0].Dependent);
    }

    // Cycle 2: SubscriberId and SubscriberName
    [Theory]
    [InlineData(0, "123456789A", "JANE DOE")]
    [InlineData(1, "987654321B", "JOHN SMITH")]
    public void X270Subscriber_exposes_SubscriberId_and_SubscriberName(int idx, string id, string name)
    {
        var doc = ParseFixture();
        Assert.Equal(id,   doc.Subscribers[idx].SubscriberId);
        Assert.Equal(name, doc.Subscribers[idx].SubscriberName);
    }

    // Cycle 4: EQ service type codes on subscriber
    [Fact]
    public void X270ServiceTypeQuery_exposes_raw_EQ_code()
    {
        var doc = ParseFixture();
        var sub0 = doc.Subscribers[0];
        Assert.Equal(3, sub0.ServiceTypeQueries.Count);
        Assert.Equal("30", sub0.ServiceTypeQueries[0].ServiceTypeCode);
        Assert.Equal("98", sub0.ServiceTypeQueries[1].ServiceTypeCode);
        Assert.Equal("AL", sub0.ServiceTypeQueries[2].ServiceTypeCode);
    }

    // Cycle 4b: EQ codes on dependent
    [Fact]
    public void X270Dependent_exposes_EQ_codes()
    {
        var doc = ParseFixture();
        var dep = doc.Subscribers[1].Dependent!;
        Assert.Equal(2, dep.ServiceTypeQueries.Count);
        Assert.Equal("30", dep.ServiceTypeQueries[0].ServiceTypeCode);
        Assert.Equal("MH", dep.ServiceTypeQueries[1].ServiceTypeCode);
    }
}
