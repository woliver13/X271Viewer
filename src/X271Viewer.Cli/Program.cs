using System.Diagnostics;
using X271Viewer.Application;

if (args.Length >= 1 && args[0].Equals("view", StringComparison.OrdinalIgnoreCase))
{
    var filePath = args.Length > 1 ? args[1] : null;
    var wpfExe  = Path.Combine(AppContext.BaseDirectory, "X271Viewer.Wpf.exe");

    if (!File.Exists(wpfExe))
    {
        Console.Error.WriteLine($"Error: WPF viewer not found at {wpfExe}");
        return 1;
    }

    var psi = new ProcessStartInfo(wpfExe) { UseShellExecute = true };
    if (!string.IsNullOrEmpty(filePath)) psi.Arguments = $"\"{filePath}\"";
    Process.Start(psi);
    return 0;
}

return CliRunner.Run(args, Console.Out, Console.Error);
