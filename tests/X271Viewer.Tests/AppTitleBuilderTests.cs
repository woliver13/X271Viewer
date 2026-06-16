using woliver13.X271Viewer.Application;

namespace woliver13.X271Viewer.Tests;

public class AppTitleBuilderTests
{
    [Fact]
    public void AppTitle_Contains_version_string()
    {
        var title = AppTitleBuilder.Build("2026.6.13.15");
        Assert.Equal("X271 Viewer 2026.6.13.15", title);
    }

    [Fact]
    public void AppTitle_Version_matches_date_pattern()
    {
        var version = AppTitleBuilder.GetFileVersion();
        Assert.False(string.IsNullOrEmpty(version));
        Assert.Matches(@"^\d{4}\.\d+\.\d+\.\d+$", version);
    }
}
