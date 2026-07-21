using MessagesEncrypter.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace MessagesEncrypter.Pages.Views;

public sealed partial class DecryptView : UserControl
{
    public DecryptView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? DecryptRequested;

    public event RoutedEventHandler? PasteEncryptedMessageRequested;

    public event EventHandler<KeyEntry?>? SelectedPrivateKeyChanged;

    public object? PrivateKeysSource
    {
        get => PrivateKeyComboBox.ItemsSource;
        set => PrivateKeyComboBox.ItemsSource = value;
    }

    public KeyEntry? SelectedPrivateKey => PrivateKeyComboBox.SelectedItem as KeyEntry;

    public int SelectedPrivateKeyIndex
    {
        get => PrivateKeyComboBox.SelectedIndex;
        set => PrivateKeyComboBox.SelectedIndex = value;
    }

    public string CipherText
    {
        get => CipherTextBox.Text;
        set => CipherTextBox.Text = value;
    }

    public string DecryptedMessage
    {
        get => DecryptedMessageTextBox.Text;
        set => DecryptedMessageTextBox.Text = value;
    }

    public void SelectPrivateKey(KeyEntry entry)
    {
        PrivateKeyComboBox.SelectedItem = entry;
    }

    private void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        DecryptRequested?.Invoke(sender, e);
    }

    private void PasteEncryptedMessageButton_Click(object sender, RoutedEventArgs e)
    {
        PasteEncryptedMessageRequested?.Invoke(sender, e);
    }

    private void PrivateKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is KeyEntry selectedKey)
        {
            SelectedPrivateKeyChanged?.Invoke(this, selectedKey);
        }
    }

    private void ClearDecryptContentButton_Click(object sender, RoutedEventArgs e)
    {
        CipherTextBox.Text = string.Empty;
        DecryptedMessageTextBox.Text = string.Empty;
    }
}
