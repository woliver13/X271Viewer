using System.Reflection;
using System.Text.Json;

namespace woliver13.X12Viewer.Domain;

public static class X12CodeTable
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _tables;

    static X12CodeTable()
    {
        var asm  = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
                      .Single(n => n.EndsWith("X12CodeTables.json"));
        using var stream = asm.GetManifestResourceStream(name)!;
        var outer = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
        _tables = outer.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, string>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    public static string Resolve(string tableName, string code)
    {
        if (_tables.TryGetValue(tableName, out var table) &&
            table.TryGetValue(code, out var label))
            return label;

        return $"{code} (unrecognized code)";
    }

    public static IReadOnlyDictionary<string, string> GetTable(string tableName) =>
        _tables.TryGetValue(tableName, out var t) ? t : new Dictionary<string, string>();
}
