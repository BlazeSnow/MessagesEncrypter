using MessagesEncrypter.Models;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MessagesEncrypter.Views;

public sealed partial class PrivateKeysView : UserControl
{
    private KeyEntry? _selectedKey;

    public PrivateKeysView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? GenerateRequested;

    public event RoutedEventHandler? ImportRequested;

    public event RoutedEventHandler? CopyPublicKeyRequested;

    public event RoutedEventHandler? ExportPublicKeyRequested;

    public event RoutedEventHandler? CopyPrivateKeyRequested;

    public event RoutedEventHandler? ExportPrivateKeyRequested;

    public event RoutedEventHandler? ChangePasswordRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event RoutedEventHandler? DeleteRequested;

    public object? ItemsSource
    {
        get => PrivateKeysItemsControl.ItemsSource;
        set => PrivateKeysItemsControl.ItemsSource = value;
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

    private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateRequested?.Invoke(sender, e);
    }

    private void ImportPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested?.Invoke(sender, e);
    }

    private void SelectPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
    }

    private void CopySelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        CopyPublicKeyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        ExportPublicKeyRequested?.Invoke(sender, e);
    }

    private void CopySelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        CopyPrivateKeyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        ExportPrivateKeyRequested?.Invoke(sender, e);
    }

    private void ChangePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        SelectKeyFromSender(sender);
        ChangePasswordRequested?.Invoke(sender, e);
    }

    private void OpenExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExportFolderRequested?.Invoke(sender, e);
    }

    private void DeleteSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
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
