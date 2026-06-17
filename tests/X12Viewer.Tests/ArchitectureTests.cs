using System.Reflection;

namespace woliver13.X12Viewer.Tests;

public class ArchitectureTests
{
    [Theory]
    [InlineData(typeof(woliver13.X12Viewer.Domain.X271Document))]
    [InlineData(typeof(woliver13.X12Viewer.Application.Placeholder))]
    public void Assembly_has_no_wpf_references(Type typeInAssembly)
    {
        var assembly = typeInAssembly.Assembly;
        var wpfRefs = assembly.GetReferencedAssemblies()
            .Where(a => a.Name != null &&
                        (a.Name.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                         a.Name.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                         a.Name.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(wpfRefs);
    }
}
