using System.Diagnostics;
using System.Reflection;

namespace woliver13.X271Viewer.Application;

public static class AppTitleBuilder
{
    private const string AppName = "X271 Viewer";

    public static string GetFileVersion() =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? string.Empty;

    public static string Build(string version) => $"{AppName} {version}";
}
