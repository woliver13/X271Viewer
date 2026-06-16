using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
    private bool                  _searchPlaceholderActive = true;

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
        var query = ActiveSearchQuery();
        PopulateTreeWithQuery(root, query);
    }

    private void PopulateTreeWithQuery(X271Node root, string query)
    {
        TreePane.Items.Clear();

        IReadOnlyList<X271Node> topNodes = [root];
        var filtered = X271NodeFilter.Filter(topNodes, query);

        NoResultsLabel.Visibility = (!string.IsNullOrWhiteSpace(query) && filtered.Count == 0)
            ? Visibility.Visible
            : Visibility.Collapsed;

        foreach (var node in filtered)
            TreePane.Items.Add(BuildTreeItem(node, query));
    }

    private static TreeViewItem BuildTreeItem(X271Node node, string query = "")
    {
        bool isFiltering = !string.IsNullOrWhiteSpace(query);
        var item = new TreeViewItem
        {
            Tag        = node,
            IsExpanded = isFiltering || !node.IsCollapsedByDefault,
        };

        item.Header = BuildHeader(node, query);

        foreach (var child in node.Children)
            item.Items.Add(BuildTreeItem(child, query));
        return item;
    }

    private static object BuildHeader(X271Node node, string query)
    {
        if (node.HasValidationErrors)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text       = "⚠ ",
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
            });
            stack.Children.Add(BuildHighlightedTextBlock(node.Label, query));
            return stack;
        }
        return BuildHighlightedTextBlock(node.Label, query);
    }

    private static TextBlock BuildHighlightedTextBlock(string text, string query)
    {
        var tb = new TextBlock();
        if (string.IsNullOrWhiteSpace(query))
        {
            tb.Text = text;
            return tb;
        }

        int idx = 0;
        while (idx < text.Length)
        {
            int match = text.IndexOf(query, idx, StringComparison.OrdinalIgnoreCase);
            if (match < 0)
            {
                tb.Inlines.Add(new Run(text[idx..]));
                break;
            }
            if (match > idx)
                tb.Inlines.Add(new Run(text[idx..match]));

            tb.Inlines.Add(new Run(text[match..(match + query.Length)])
            {
                Background = Brushes.Yellow,
                FontWeight = FontWeights.Bold,
            });
            idx = match + query.Length;
        }
        return tb;
    }

    private string ActiveSearchQuery()
    {
        return _searchPlaceholderActive ? string.Empty : SearchBox.Text;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchPlaceholderActive) return;
        if (_currentRoot is null) return;
        PopulateTreeWithQuery(_currentRoot, SearchBox.Text);
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_searchPlaceholderActive)
        {
            _searchPlaceholderActive = false;
            SearchBox.Text = string.Empty;
            SearchBox.Foreground = SystemColors.WindowTextBrush;
        }
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            _searchPlaceholderActive = true;
            SearchBox.Text = "Search…";
            SearchBox.Foreground = Brushes.Gray;
        }
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
