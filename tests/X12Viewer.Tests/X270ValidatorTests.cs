using woliver13.X12Viewer.Application;

namespace woliver13.X12Viewer.Tests;

public class X270ValidatorTests
{
    private static readonly string FixtureDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static string FixturePath(string name) => Path.Combine(FixtureDir, name);

    // Cycle 7: zero errors for well-formed fixture
    [Fact]
    public void X270Validator_returns_zero_errors_for_well_formed_fixture()
    {
        var content = File.ReadAllText(FixturePath("tests270.edi"));
        var errors = X270Validator.Validate(content);
        Assert.Empty(errors);
    }

    // Cycle 8: missing subscriber loop → error
    [Fact]
    public void X270Validator_missing_subscriber_loop_returns_error()
    {
        // 270 without any HL*...*22 loop
        var content = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~GS*HS*SENDER*RECEIVER*20230101*1200*1*X*005010X279A1~ST*270*0001~BHT*0022*13*ABC1234*20230101*1200~HL*1**20*1~SE*5*0001~GE*1*1~IEA*1*000000001~";
        var errors = X270Validator.Validate(content);
        Assert.NotEmpty(errors);
    }

    // Cycle 9: no EQ segments → error
    [Fact]
    public void X270Validator_no_EQ_segments_returns_error()
    {
        // 270 with subscriber loop but no EQ segments
        var content = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~GS*HS*SENDER*RECEIVER*20230101*1200*1*X*005010X279A1~ST*270*0001~BHT*0022*13*ABC1234*20230101*1200~HL*1**20*1~HL*2*1*21*1~HL*3*2*22*0~NM1*IL*1*DOE*JANE****MI*123~SE*8*0001~GE*1*1~IEA*1*000000001~";
        var errors = X270Validator.Validate(content);
        Assert.NotEmpty(errors);
    }
}
