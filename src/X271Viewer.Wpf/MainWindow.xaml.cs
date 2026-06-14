using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using X271Viewer.Application;
using X271Viewer.Domain;

namespace X271Viewer.Wpf;

public partial class MainWindow : Window
{
    public static readonly RoutedUICommand ExportCommand = new(
        "Export JSON…", "ExportCommand", typeof(MainWindow),
        [new KeyGesture(Key.E, ModifierKeys.Control)]);

    private readonly X271DocumentParser    _parser    = new();
    private readonly X271ValidationService _validator = new();

    private X271Node?             _currentRoot             = null;
    private X271ValidationResult? _currentValidationResult = null;
    private string                _currentIsaRawText       = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        Title = AppTitleBuilder.Build(AppTitleBuilder.GetFileVersion());

        CommandBindings.Add(new CommandBinding(
            ExportCommand,
            Export_Click,
            (s, e) => e.CanExecute = _currentRoot is not null));
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
        OpenFile(dlg.FileName);
    }

    public void OpenFile(string path)
    {
        try
        {
            var content = File.ReadAllText(path);
            var doc     = _parser.ParseContent(content);
            var root    = X271TreeBuilder.Build(doc);

            var validationResult = _validator.Validate(content);
            _validator.AnnotateTree(root, validationResult);

            _currentRoot             = root;
            _currentValidationResult = validationResult;
            _currentIsaRawText       = doc.IsaRawText;

            PopulateTree(root);
            RawSegmentPane.Text = doc.IsaRawText;

            if (validationResult.IsValid)
            {
                InterpretationPane.Text      = "✓ No validation errors.";
                InterpretationPane.FontStyle  = FontStyles.Normal;
                InterpretationPane.Foreground = Brushes.DarkGreen;
            }
            else
            {
                InterpretationPane.Text      = "Select a node to see its plain-English interpretation.";
                InterpretationPane.FontStyle  = FontStyles.Italic;
                InterpretationPane.Foreground = Brushes.Gray;
            }
        }
        catch (X271ParseException ex)
        {
            MessageBox.Show(ex.Message, "Cannot Open File", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PopulateTree(X271Node root)
    {
        TreePane.Items.Clear();
        TreePane.Items.Add(BuildTreeItem(root));
    }

    private static TreeViewItem BuildTreeItem(X271Node node)
    {
        var item = new TreeViewItem
        {
            Tag        = node,
            IsExpanded = !node.IsCollapsedByDefault,
        };

        if (node.HasValidationErrors)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text       = "⚠ ",
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
            });
            stack.Children.Add(new TextBlock { Text = node.Label });
            item.Header = stack;
        }
        else
        {
            item.Header = node.Label;
        }

        foreach (var child in node.Children)
            item.Items.Add(BuildTreeItem(child));
        return item;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRoot is null || _currentValidationResult is null)
        {
            MessageBox.Show("Open a 271 file first.", "Nothing to Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title       = "Export 271 as JSON",
            Filter      = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FilterIndex = 1,
            DefaultExt  = ".json",
            FileName    = "export.json",
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var exportDoc = X271JsonExporter.BuildExportDocument(
                _currentRoot, _currentValidationResult, _currentIsaRawText);
            var json = X271JsonExporter.Export(exportDoc);
            File.WriteAllText(dlg.FileName, json);
            MessageBox.Show($"Exported to:{Environment.NewLine}{dlg.FileName}",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TreePane_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem { Tag: X271Node node }) return;

        RawSegmentPane.Text = string.Join(Environment.NewLine, node.RawSegments);

        if (node.HasValidationErrors)
        {
            var errorSummary = string.Join(Environment.NewLine,
                node.ValidationErrors.Select((msg, i) => $"• {msg}"));
            InterpretationPane.Text      = $"Validation errors:{Environment.NewLine}{errorSummary}";
            InterpretationPane.FontStyle  = FontStyles.Normal;
            InterpretationPane.Foreground = Brushes.Red;
        }
        else
        {
            InterpretationPane.Text      = X271InterpretationEngine.Interpret(node);
            InterpretationPane.FontStyle  = FontStyles.Normal;
            InterpretationPane.Foreground = Brushes.Black;
        }
    }
}
