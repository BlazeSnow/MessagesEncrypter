using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Models;

namespace MessagesEncrypter.Views;

public sealed partial class PrivateKeysView : UserControl
{
    public PrivateKeysView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? GenerateRequested;

    public event RoutedEventHandler? CopyPublicKeyRequested;

    public event RoutedEventHandler? ExportPublicKeyRequested;

    public event RoutedEventHandler? CopyPrivateKeyRequested;

    public event RoutedEventHandler? ExportPrivateKeyRequested;

    public event RoutedEventHandler? DeleteRequested;

    public object? ItemsSource
    {
        get => PrivateKeysListView.ItemsSource;
        set => PrivateKeysListView.ItemsSource = value;
    }

    public KeyEntry? SelectedKey => PrivateKeysListView.SelectedItem as KeyEntry;

    public int SelectedIndex
    {
        get => PrivateKeysListView.SelectedIndex;
        set => PrivateKeysListView.SelectedIndex = value;
    }

    public void SelectKey(KeyEntry entry)
    {
        PrivateKeysListView.SelectedItem = entry;
    }

    private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateRequested?.Invoke(sender, e);
    }

    private void CopySelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyPublicKeyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ExportPublicKeyRequested?.Invoke(sender, e);
    }

    private void CopySelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyPrivateKeyRequested?.Invoke(sender, e);
    }

    private void ExportSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        ExportPrivateKeyRequested?.Invoke(sender, e);
    }

    private void DeleteSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(sender, e);
    }
}
