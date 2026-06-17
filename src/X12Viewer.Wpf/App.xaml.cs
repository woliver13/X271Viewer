using System.Windows;

namespace woliver13.X12Viewer.Wpf;

public partial class App : System.Windows.Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        var window = new MainWindow();
        window.Show();

        if (e.Args.Length > 0 && System.IO.File.Exists(e.Args[0]))
            window.OpenFile(e.Args[0]);
    }
}
