using MessagesEncrypter.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;

namespace MessagesEncrypter.Pages.Views;

public sealed partial class RecipientKeysView : UserControl
{
    private KeyEntry? _selectedKey;

    public RecipientKeysView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? ImportRequested;

    public event RoutedEventHandler? CopyRequested;

    public event RoutedEventHandler? ExportRequested;

    public event RoutedEventHandler? RenameRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event RoutedEventHandler? DeleteRequested;

    public object? ItemsSource
    {
        get => RecipientKeysItemsControl.ItemsSource;
        set => RecipientKeysItemsControl.ItemsSource = value;
    }

    public KeyEntry? SelectedKey => _selectedKey;

    public int SelectedIndex
    {
        get => ItemsSource is IList list && _selectedKey is not null ? list.IndexOf(_selectedKey) : -1;
        set
        {
            if (ItemsSource is IList list && value >= 0 && value < list.Count)
            {
                _selectedKey = list[value] as KeyEntry;
            }
            else
            {
                _selectedKey = null;
            }
        }
    }

    public void SelectKey(KeyEntry entry)
    {
        _selectedKey = entry;
    }

    private void ImportRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested?.Invoke(sender, e);
    }

    private void SelectRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
    }

    private void CopySelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        CopyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        ExportRequested?.Invoke(sender, e);
    }

    private void RenameSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        RenameRequested?.Invoke(sender, e);
    }

    private void OpenExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExportFolderRequested?.Invoke(sender, e);
    }

    private void DeleteSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        DeleteRequested?.Invoke(sender, e);
    }

    private void SelectKeyFromSender(object sender)
    {
        if (sender is FrameworkElement { Tag: KeyEntry entry })
        {
            _selectedKey = entry;
        }
    }
}
