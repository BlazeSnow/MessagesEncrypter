using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Services;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace MessagesEncrypter
{
    public sealed partial class MainWindow : Window
    {
        private readonly KeyManagementService _keyManagementService = new();
        private readonly MessageCryptoService _messageCryptoService;

        public MainWindow()
        {
            _messageCryptoService = new MessageCryptoService(_keyManagementService);
            InitializeComponent();
            Title = AppResources.GetString("MainWindowTitle");
            ShowPanel("Encrypt");
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                ShowPanel(tag);
            }
        }

        private void ShowPanel(string tag)
        {
            EncryptPanel.Visibility = tag == "Encrypt" ? Visibility.Visible : Visibility.Collapsed;
            DecryptPanel.Visibility = tag == "Decrypt" ? Visibility.Visible : Visibility.Collapsed;
            KeysPanel.Visibility = tag == "Keys" ? Visibility.Visible : Visibility.Collapsed;
            FilesPanel.Visibility = tag == "Files" ? Visibility.Visible : Visibility.Collapsed;
            SecurityPanel.Visibility = tag == "Security" ? Visibility.Visible : Visibility.Collapsed;
            PageTitleText.Text = AppResources.GetString($"PageTitle{tag}");
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KeyPairResult result = _keyManagementService.GenerateKeyPair(KeyPasswordBox.Password);
                GeneratedPublicKeyTextBox.Text = result.PublicKeyPem;
                GeneratedPrivateKeyTextBox.Text = result.EncryptedPrivateKeyPem;
                FingerprintTextBox.Text = result.PublicKeyFingerprint;
                EncryptPublicKeyTextBox.Text = result.PublicKeyPem;
                ShowStatus("StatusKeyGenerated", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EncryptedMessageTextBox.Text = _messageCryptoService.EncryptToBase64Json(
                    PlainTextBox.Text,
                    EncryptPublicKeyTextBox.Text);
                ShowStatus("StatusMessageEncrypted", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DecryptedMessageTextBox.Text = _messageCryptoService.DecryptFromBase64Json(
                    CipherTextBox.Text,
                    DecryptPrivateKeyTextBox.Text,
                    DecryptPasswordBox.Password);
                ShowStatus("StatusMessageDecrypted", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                DecryptedMessageTextBox.Text = string.Empty;
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void CopyEncryptedMessageButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(EncryptedMessageTextBox.Text, "StatusEncryptedMessageCopied");
        }

        private void CopyPublicKeyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(GeneratedPublicKeyTextBox.Text, "StatusPublicKeyCopied");
        }

        private void CopyPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(GeneratedPrivateKeyTextBox.Text, "StatusPrivateKeyCopied");
        }

        private async void PasteEncryptedMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataPackageView content = Clipboard.GetContent();
                if (content.Contains(StandardDataFormats.Text))
                {
                    CipherTextBox.Text = await content.GetTextAsync();
                    ShowStatus("StatusEncryptedMessagePasted", InfoBarSeverity.Success);
                    return;
                }

                ShowStatus("ErrorClipboardTextMissing", InfoBarSeverity.Warning);
            }
            catch
            {
                ShowStatus("ErrorClipboardUnavailable", InfoBarSeverity.Error);
            }
        }

        private void CopyTextToClipboard(string text, string successResourceKey)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowStatus("ErrorClipboardNothingToCopy", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                var package = new DataPackage();
                package.SetText(text);
                Clipboard.SetContent(package);
                ShowStatus(successResourceKey, InfoBarSeverity.Success);
            }
            catch
            {
                ShowStatus("ErrorClipboardUnavailable", InfoBarSeverity.Error);
            }
        }

        private void ShowStatus(string resourceKey, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = AppResources.GetString(resourceKey);
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }
    }
}
