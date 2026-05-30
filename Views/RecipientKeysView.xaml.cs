using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Models;

namespace MessagesEncrypter.Views;

public sealed partial class RecipientKeysView : UserControl
{
    public RecipientKeysView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? ImportRequested;

    public event RoutedEventHandler? CopyRequested;

    public event RoutedEventHandler? ExportRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event RoutedEventHandler? DeleteRequested;

    public object? ItemsSource
    {
        get => RecipientKeysListView.ItemsSource;
        set => RecipientKeysListView.ItemsSource = value;
    }

    public KeyEntry? SelectedKey => RecipientKeysListView.SelectedItem as KeyEntry;

    public int SelectedIndex
    {
        get => RecipientKeysListView.SelectedIndex;
        set => RecipientKeysListView.SelectedIndex = value;
    }

    public void SelectKey(KeyEntry entry)
    {
        RecipientKeysListView.SelectedItem = entry;
    }

    private void ImportRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested?.Invoke(sender, e);
    }

    private void CopySelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ExportRequested?.Invoke(sender, e);
    }

    private void OpenExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExportFolderRequested?.Invoke(sender, e);
    }

    private void DeleteSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(sender, e);
    }
}
