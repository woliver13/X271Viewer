using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using woliver13.X12Viewer.Application;
using woliver13.X12Viewer.Domain;

namespace woliver13.X12Viewer.Wpf;

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
    private bool                  _searchPlaceholderActive  = true;
    private bool                  _is835Loaded              = false;
    private bool                  _is270Loaded              = false;
    private string?               _current835FilePath       = null;
    private string                _835ValidationText        = string.Empty;
    private Brush                 _835ValidationBrush       = Brushes.DarkGreen;

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
            Title = "Open X12 EDI File",
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

            // Detect transaction type via ST01
            var st01 = DetectSt01(content);

            if (st01 == "835")
            {
                _current835FilePath = path;
                Open835File(content);
            }
            else if (st01 == "270")
            {
                _current835FilePath = null;
                Open270File(content);
            }
            else if (st01 is "277" or "276")
            {
                _current835FilePath = null;
                Open277File(content);
            }
            else
            {
                _current835FilePath = null;
                Open271File(content);
            }
        }
        catch (X271ParseException ex)
        {
            MessageBox.Show(ex.Message, "Cannot Open File", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string DetectSt01(string content)
    {
        // Find "ST*XXX*" pattern — ST01 is the transaction set identifier
        var stIdx = content.IndexOf("ST*", StringComparison.Ordinal);
        if (stIdx < 0) return "";
        var afterSt  = content[(stIdx + 3)..];
        var starIdx  = afterSt.IndexOf('*');
        var tildeIdx = afterSt.IndexOf('~');
        var end      = starIdx >= 0 ? starIdx : tildeIdx;
        return end >= 0 ? afterSt[..end] : "";
    }

    private void Open835File(string content)
    {
        var doc    = new X835DocumentParser().ParseContent(content);
        var root   = X835TreeBuilder.Build(doc);
        var issues = new X835Validator().Validate(doc);

        _currentRoot             = root;
        _currentValidationResult = null;
        _currentIsaRawText       = doc.IsaRawText;
        _is835Loaded             = true;
        UpdateExportMenuState();

        PopulateTree(root);
        RawSegmentPane.Text = FormatEdiForDisplay(content);

        if (issues.Count == 0)
        {
            _835ValidationText  = "✓ No validation issues.";
            _835ValidationBrush = Brushes.DarkGreen;
        }
        else
        {
            _835ValidationText  = $"Validation issues:{Environment.NewLine}" +
                string.Join(Environment.NewLine, issues.Select(i => $"• {i}"));
            _835ValidationBrush = Brushes.DarkRed;
        }

        InterpretationPane.Text      = _835ValidationText;
        InterpretationPane.FontStyle  = FontStyles.Normal;
        InterpretationPane.Foreground = _835ValidationBrush;
    }

    private void Open277File(string content)
    {
        _is835Loaded = false;
        _is270Loaded = false;
        UpdateExportMenuState();

        var doc277      = new X277DocumentParser().ParseContent(content);
        var enriched    = X277Interpreter.Interpret(doc277);
        var root        = X277TreeBuilder.Build(enriched);

        _currentRoot             = root;
        _currentValidationResult = null;
        _currentIsaRawText       = content;

        PopulateTree(root);
        RawSegmentPane.Text = FormatEdiForDisplay(content);

        InterpretationPane.Text      = "✓ Claim status loaded.";
        InterpretationPane.FontStyle  = FontStyles.Normal;
        InterpretationPane.Foreground = Brushes.DarkGreen;
    }

    private void UpdateExportMenuState()
    {
        ExportCsvMenuItem.IsEnabled = _is835Loaded;
    }

    private void Open270File(string content)
    {
        _is835Loaded = false;
        _is270Loaded = true;
        UpdateExportMenuState();

        var raw  = new X270DocumentParser().ParseContent(content);
        var doc  = X270Interpreter.Interpret(raw);
        var root = X270TreeBuilder.Build(doc);

        _currentRoot             = root;
        _currentValidationResult = null;
        _currentIsaRawText       = content;

        PopulateTree(root);
        RawSegmentPane.Text          = content;
        InterpretationPane.Text      = "Select a node to see its plain-English interpretation.";
        InterpretationPane.FontStyle  = FontStyles.Italic;
        InterpretationPane.Foreground = Brushes.Gray;
    }

    private void Open271File(string content)
    {
        _is835Loaded = false;
        _is270Loaded = false;
        UpdateExportMenuState();
        var doc    = _parser.ParseContent(content);
        var root   = X271TreeBuilder.Build(doc);

        var validationResult = _validator.Validate(content);
        _validator.AnnotateTree(root, validationResult);

        _currentRoot             = root;
        _currentValidationResult = validationResult;
        _currentIsaRawText       = doc.IsaRawText;

        PopulateTree(root);
        RawSegmentPane.Text = FormatEdiForDisplay(doc.IsaRawText);

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
        if (_currentRoot is null)
        {
            MessageBox.Show("Open a file first.", "Nothing to Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_currentValidationResult is null)
        {
            MessageBox.Show("Export is not available for 835 files yet.", "Export Not Supported",
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

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRoot is null || !_is835Loaded) return;
        var dlg = new SaveFileDialog
        {
            Title      = "Export 835 as CSV",
            Filter     = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName   = "export835.csv",
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var doc = new X835DocumentParser().ParseFile(_current835FilePath!);
            var csv = X835CsvExporter.Export(doc);
            File.WriteAllText(dlg.FileName, csv, System.Text.Encoding.UTF8);
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

        // 270/835 nodes carry no raw segments — restore the full content and show node summary
        if (node.RawSegments.Count == 0 && _is270Loaded)
        {
            RawSegmentPane.Text          = FormatEdiForDisplay(_currentIsaRawText);
            InterpretationPane.Text      = node.Label;
            InterpretationPane.FontStyle  = FontStyles.Normal;
            InterpretationPane.Foreground = Brushes.Black;
            return;
        }

        if (node.RawSegments.Count == 0 && _is835Loaded)
        {
            InterpretationPane.Text      = _835ValidationText;
            InterpretationPane.FontStyle  = FontStyles.Normal;
            InterpretationPane.Foreground = _835ValidationBrush;
            return;
        }

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

    private static string FormatEdiForDisplay(string content)
        => content.Replace("~", "~" + Environment.NewLine).TrimEnd();
}
