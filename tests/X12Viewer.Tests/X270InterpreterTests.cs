using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Tests;

public class X270InterpreterTests
{
    // Cycle 5: known EQ code 30 resolves to non-empty description
    [Fact]
    public void X270Interpreter_resolves_known_EQ_code_to_nonempty_description()
    {
        var sub = new X270Subscriber("ID", "NAME", [new X270ServiceTypeQuery("30")]);
        var doc = new X270Document { Subscribers = [sub] };
        var result = X270Interpreter.Interpret(doc);
        Assert.False(string.IsNullOrEmpty(result.Subscribers[0].ServiceTypeQueries[0].Description));
        Assert.DoesNotContain("unrecognized", result.Subscribers[0].ServiceTypeQueries[0].Description);
    }

    // Cycle 6: unknown EQ code falls back to "{code} (unrecognized code)"
    [Fact]
    public void X270Interpreter_unknown_code_falls_back_to_unrecognized_suffix()
    {
        var sub = new X270Subscriber("ID", "NAME", [new X270ServiceTypeQuery("ZZZZZ")]);
        var doc = new X270Document { Subscribers = [sub] };
        var result = X270Interpreter.Interpret(doc);
        Assert.Equal("ZZZZZ (unrecognized code)", result.Subscribers[0].ServiceTypeQueries[0].Description);
    }
}
