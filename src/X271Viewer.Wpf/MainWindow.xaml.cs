using System.Windows;
using Microsoft.Win32;
using X271Viewer.Domain;

namespace X271Viewer.Wpf;

public partial class MainWindow : Window
{
    private readonly X271DocumentParser _parser = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open X12 271 File",
            Filter = "EDI files (*.edi;*.txt;*.x12)|*.edi;*.txt;*.x12|All files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var doc = _parser.ParseFile(dlg.FileName);
            RawSegmentPane.Text = doc.IsaRawText;
        }
        catch (X271ParseException ex)
        {
            MessageBox.Show(ex.Message, "Cannot Open File", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
