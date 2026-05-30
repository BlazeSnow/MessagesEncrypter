using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Models;

namespace MessagesEncrypter.Views;

public sealed partial class EncryptView : UserControl
{
    public EncryptView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? EncryptRequested;

    public event RoutedEventHandler? CopyEncryptedMessageRequested;

    public object? RecipientKeysSource
    {
        get => RecipientKeyComboBox.ItemsSource;
        set => RecipientKeyComboBox.ItemsSource = value;
    }

    public KeyEntry? SelectedRecipientKey => RecipientKeyComboBox.SelectedItem as KeyEntry;

    public int SelectedRecipientIndex
    {
        get => RecipientKeyComboBox.SelectedIndex;
        set => RecipientKeyComboBox.SelectedIndex = value;
    }

    public string PlainText => PlainTextBox.Text;

    public string EncryptedMessage
    {
        get => EncryptedMessageTextBox.Text;
        set => EncryptedMessageTextBox.Text = value;
    }

    public void SelectRecipientKey(KeyEntry entry)
    {
        RecipientKeyComboBox.SelectedItem = entry;
    }

    private void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        EncryptRequested?.Invoke(sender, e);
    }

    private void CopyEncryptedMessageButton_Click(object sender, RoutedEventArgs e)
    {
        CopyEncryptedMessageRequested?.Invoke(sender, e);
    }

    private void ClearEncryptContentButton_Click(object sender, RoutedEventArgs e)
    {
        PlainTextBox.Text = string.Empty;
        EncryptedMessageTextBox.Text = string.Empty;
    }
}
