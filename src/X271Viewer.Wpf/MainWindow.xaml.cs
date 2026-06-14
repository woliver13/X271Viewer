using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using X271Viewer.Application;
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
            var doc  = _parser.ParseFile(dlg.FileName);
            var root = X271TreeBuilder.Build(doc);
            PopulateTree(root);
            RawSegmentPane.Text = doc.IsaRawText;
            InterpretationPane.Text = "Select a node to see its plain-English interpretation.";
            InterpretationPane.FontStyle = FontStyles.Italic;
            InterpretationPane.Foreground = System.Windows.Media.Brushes.Gray;
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
            Header     = node.Label,
            Tag        = node,
            IsExpanded = !node.IsCollapsedByDefault,
        };
        foreach (var child in node.Children)
            item.Items.Add(BuildTreeItem(child));
        return item;
    }

    private void TreePane_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem { Tag: X271Node node }) return;
        RawSegmentPane.Text = string.Join(Environment.NewLine, node.RawSegments);
        InterpretationPane.Text = X271InterpretationEngine.Interpret(node);
        InterpretationPane.FontStyle = FontStyles.Normal;
        InterpretationPane.Foreground = System.Windows.Media.Brushes.Black;
    }
}
