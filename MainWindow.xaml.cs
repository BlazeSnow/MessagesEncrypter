using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Models;
using MessagesEncrypter.Services;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace MessagesEncrypter
{
    public sealed partial class MainWindow : Window
    {
        private readonly KeyManagementService _keyManagementService = new();
        private readonly CredentialManagerService _credentialManagerService = new();
        private readonly MessageCryptoService _messageCryptoService;
        private readonly ObservableCollection<KeyEntry> _recipientKeys = [];
        private readonly ObservableCollection<KeyEntry> _privateKeys = [];

        public MainWindow()
        {
            _messageCryptoService = new MessageCryptoService(_keyManagementService);
            InitializeComponent();
            Title = AppResources.GetString("MainWindowTitle");
            RecipientKeyComboBox.ItemsSource = _recipientKeys;
            RecipientKeysListView.ItemsSource = _recipientKeys;
            PrivateKeyComboBox.ItemsSource = _privateKeys;
            PrivateKeysListView.ItemsSource = _privateKeys;
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
            SettingsPanel.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            PageTitleText.Text = AppResources.GetString($"PageTitle{tag}");
        }

        private async void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox aliasTextBox = CreateDialogTextBox("PrivateKeyAliasTextBox");
            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "GeneratePrivateKeyDialogTitle",
                "GeneratePrivateKeyDialogPrimaryButtonText",
                aliasTextBox);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                string password = _credentialManagerService.GetPrivateKeyPassword();
                KeyPairResult result = _keyManagementService.GenerateKeyPair(password);
                string alias = GetAliasOrDefault(aliasTextBox.Text, "DefaultPrivateKeyAlias", _privateKeys.Count + 1);
                var entry = new KeyEntry(alias, result.PublicKeyFingerprint, result.PublicKeyPem, result.EncryptedPrivateKeyPem);
                _privateKeys.Add(entry);
                PrivateKeyComboBox.SelectedItem = entry;
                PrivateKeysListView.SelectedItem = entry;
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
                if (RecipientKeyComboBox.SelectedItem is not KeyEntry recipientKey || string.IsNullOrWhiteSpace(recipientKey.PublicKeyPem))
                {
                    ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                EncryptedMessageTextBox.Text = _messageCryptoService.EncryptToBase64Json(
                    PlainTextBox.Text,
                    recipientKey.PublicKeyPem);
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
                if (PrivateKeyComboBox.SelectedItem is not KeyEntry privateKey || string.IsNullOrWhiteSpace(privateKey.EncryptedPrivateKeyPem))
                {
                    ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                DecryptedMessageTextBox.Text = _messageCryptoService.DecryptFromBase64Json(
                    CipherTextBox.Text,
                    privateKey.EncryptedPrivateKeyPem,
                    _credentialManagerService.GetPrivateKeyPassword());
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

        private async void ImportRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox aliasTextBox = CreateDialogTextBox("RecipientAliasTextBox");
            TextBox publicKeyTextBox = CreateDialogTextBox("RecipientPublicKeyTextBox");
            publicKeyTextBox.AcceptsReturn = true;
            publicKeyTextBox.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
            publicKeyTextBox.Height = 180;
            publicKeyTextBox.TextWrapping = TextWrapping.NoWrap;

            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(aliasTextBox);
            dialogContent.Children.Add(publicKeyTextBox);

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "ImportRecipientKeyDialogTitle",
                "ImportRecipientKeyDialogPrimaryButtonText",
                dialogContent);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                string publicKeyPem = publicKeyTextBox.Text;
                string fingerprint = _keyManagementService.GetPublicKeyFingerprint(publicKeyPem);
                string alias = GetAliasOrDefault(aliasTextBox.Text, "DefaultRecipientKeyAlias", _recipientKeys.Count + 1);
                var entry = new KeyEntry(alias, fingerprint, publicKeyPem, null);
                _recipientKeys.Add(entry);
                RecipientKeyComboBox.SelectedItem = entry;
                RecipientKeysListView.SelectedItem = entry;
                ShowStatus("StatusRecipientKeyImported", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void CopySelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientKeysListView.SelectedItem is not KeyEntry entry)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.PublicKeyPem ?? string.Empty, "StatusPublicKeyCopied");
        }

        private void CopySelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysListView.SelectedItem is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.PublicKeyPem ?? string.Empty, "StatusPublicKeyCopied");
        }

        private void CopySelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysListView.SelectedItem is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.EncryptedPrivateKeyPem ?? string.Empty, "StatusPrivateKeyCopied");
        }

        private void SavePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _credentialManagerService.SavePrivateKeyPassword(PrivateKeyPasswordBox.Password);
                PrivateKeyPasswordBox.Password = string.Empty;
                ShowStatus("StatusPrivateKeyPasswordSaved", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void DeletePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _credentialManagerService.DeletePrivateKeyPassword();
                PrivateKeyPasswordBox.Password = string.Empty;
                ShowStatus("StatusPrivateKeyPasswordDeleted", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
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

        private static string GetAliasOrDefault(string alias, string defaultResourceKey, int index)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                return alias.Trim();
            }

            return string.Format(AppResources.GetString(defaultResourceKey), index);
        }

        private TextBox CreateDialogTextBox(string resourcePrefix)
        {
            return new TextBox
            {
                PlaceholderText = AppResources.GetString($"{resourcePrefix}.PlaceholderText")
            };
        }

        private async System.Threading.Tasks.Task<ContentDialogResult> ShowInputDialogAsync(
            string titleResourceKey,
            string primaryButtonResourceKey,
            object content)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootNavigation.XamlRoot,
                Title = AppResources.GetString(titleResourceKey),
                PrimaryButtonText = AppResources.GetString(primaryButtonResourceKey),
                CloseButtonText = AppResources.GetString("DialogCancelButtonText"),
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            return await dialog.ShowAsync();
        }
    }
}
