using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MessagesEncrypter.Models;
using MessagesEncrypter.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using WinRT.Interop;

namespace MessagesEncrypter
{
    public sealed partial class MainWindow : Window
    {
        private readonly KeyManagementService _keyManagementService = new();
        private readonly CredentialManagerService _credentialManagerService = new();
        private readonly KeyStoreService _keyStoreService = new();
        private readonly AppSettingsService _appSettingsService = new();
        private readonly KeyExportService _keyExportService = new();
        private readonly MessageCryptoService _messageCryptoService;
        private readonly ObservableCollection<KeyEntry> _recipientKeys = [];
        private readonly ObservableCollection<KeyEntry> _privateKeys = [];

        public MainWindow()
        {
            _messageCryptoService = new MessageCryptoService(_keyManagementService);
            InitializeComponent();
            Title = AppResources.GetString("MainWindowTitle");
            InitializeViews();
            LoadKeyStore();
            LoadSettings();
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
            EncryptView.Visibility = tag == "Encrypt" ? Visibility.Visible : Visibility.Collapsed;
            DecryptView.Visibility = tag == "Decrypt" ? Visibility.Visible : Visibility.Collapsed;
            RecipientKeysView.Visibility = tag == "RecipientKeys" ? Visibility.Visible : Visibility.Collapsed;
            PrivateKeysView.Visibility = tag == "PrivateKeys" ? Visibility.Visible : Visibility.Collapsed;
            FilesView.Visibility = tag == "Files" ? Visibility.Visible : Visibility.Collapsed;
            SettingsView.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            PageTitleText.Text = AppResources.GetString($"PageTitle{tag}");
        }

        private void InitializeViews()
        {
            EncryptView.RecipientKeysSource = _recipientKeys;
            EncryptView.EncryptRequested += EncryptButton_Click;
            EncryptView.CopyEncryptedMessageRequested += CopyEncryptedMessageButton_Click;

            DecryptView.PrivateKeysSource = _privateKeys;
            DecryptView.DecryptRequested += DecryptButton_Click;
            DecryptView.PasteEncryptedMessageRequested += PasteEncryptedMessageButton_Click;

            RecipientKeysView.ItemsSource = _recipientKeys;
            RecipientKeysView.ImportRequested += ImportRecipientKeyButton_Click;
            RecipientKeysView.CopyRequested += CopySelectedRecipientKeyButton_Click;
            RecipientKeysView.ExportRequested += ExportSelectedRecipientKeyButton_Click;
            RecipientKeysView.OpenExportFolderRequested += OpenExportFolderButton_Click;
            RecipientKeysView.DeleteRequested += DeleteSelectedRecipientKeyButton_Click;

            PrivateKeysView.ItemsSource = _privateKeys;
            PrivateKeysView.GenerateRequested += GenerateKeyButton_Click;
            PrivateKeysView.CopyPublicKeyRequested += CopySelectedPrivatePublicKeyButton_Click;
            PrivateKeysView.ExportPublicKeyRequested += ExportSelectedPrivatePublicKeyButton_Click;
            PrivateKeysView.CopyPrivateKeyRequested += CopySelectedPrivateKeyButton_Click;
            PrivateKeysView.ExportPrivateKeyRequested += ExportSelectedPrivateKeyButton_Click;
            PrivateKeysView.OpenExportFolderRequested += OpenExportFolderButton_Click;
            PrivateKeysView.DeleteRequested += DeleteSelectedPrivateKeyButton_Click;

            SettingsView.ChooseExportFolderRequested += ChooseExportFolderButton_Click;
            SettingsView.OpenExportFolderRequested += OpenExportFolderButton_Click;
            SettingsView.SavePrivateKeyPasswordRequested += SavePrivateKeyPasswordButton_Click;
            SettingsView.DeletePrivateKeyPasswordRequested += DeletePrivateKeyPasswordButton_Click;
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
                DecryptView.SelectPrivateKey(entry);
                PrivateKeysView.SelectKey(entry);
                if (SaveKeyStore())
                {
                    ShowStatus("StatusKeyGenerated", InfoBarSeverity.Success);
                }
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
                if (EncryptView.SelectedRecipientKey is not KeyEntry recipientKey || string.IsNullOrWhiteSpace(recipientKey.PublicKeyPem))
                {
                    ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                EncryptView.EncryptedMessage = _messageCryptoService.EncryptToBase64Json(
                    EncryptView.PlainText,
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
                if (DecryptView.SelectedPrivateKey is not KeyEntry privateKey || string.IsNullOrWhiteSpace(privateKey.EncryptedPrivateKeyPem))
                {
                    ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                DecryptView.DecryptedMessage = _messageCryptoService.DecryptFromBase64Json(
                    DecryptView.CipherText,
                    privateKey.EncryptedPrivateKeyPem,
                    _credentialManagerService.GetPrivateKeyPassword());
                ShowStatus("StatusMessageDecrypted", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                DecryptView.DecryptedMessage = string.Empty;
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void CopyEncryptedMessageButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(EncryptView.EncryptedMessage, "StatusEncryptedMessageCopied");
        }

        private async void ImportRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox aliasTextBox = CreateDialogTextBox("RecipientAliasTextBox");
            TextBox publicKeyTextBox = CreateDialogTextBox("RecipientPublicKeyTextBox");
            publicKeyTextBox.AcceptsReturn = true;
            publicKeyTextBox.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
            publicKeyTextBox.Height = 180;
            publicKeyTextBox.TextWrapping = TextWrapping.NoWrap;
            Button importFromFileButton = new()
            {
                Content = AppResources.GetString("ImportRecipientKeyFromFileButtonText")
            };
            importFromFileButton.Click += async (_, _) => await ImportRecipientPublicKeyFromFileAsync(publicKeyTextBox);

            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(aliasTextBox);
            dialogContent.Children.Add(importFromFileButton);
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
                EncryptView.SelectRecipientKey(entry);
                RecipientKeysView.SelectKey(entry);
                if (SaveKeyStore())
                {
                    ShowStatus("StatusRecipientKeyImported", InfoBarSeverity.Success);
                }
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void CopySelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.PublicKeyPem ?? string.Empty, "StatusPublicKeyCopied");
        }

        private void ExportSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            ExportKey(() => _keyExportService.ExportPublicKey(entry, _appSettingsService.GetExportFolderPath()));
        }

        private async void DeleteSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            ContentDialogResult dialogResult = await ShowConfirmDialogAsync(
                "DeleteRecipientKeyDialogTitle",
                "DeleteKeyDialogPrimaryButtonText",
                string.Format(AppResources.GetString("DeleteKeyDialogContent"), entry.DisplayName));

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            _recipientKeys.Remove(entry);
            SelectFirstRecipientKeyIfAvailable();
            if (SaveKeyStore())
            {
                ShowStatus("StatusRecipientKeyDeleted", InfoBarSeverity.Success);
            }
        }

        private void CopySelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.PublicKeyPem ?? string.Empty, "StatusPublicKeyCopied");
        }

        private void ExportSelectedPrivatePublicKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            ExportKey(() => _keyExportService.ExportPublicKey(entry, _appSettingsService.GetExportFolderPath()));
        }

        private void CopySelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            CopyTextToClipboard(entry.EncryptedPrivateKeyPem ?? string.Empty, "StatusPrivateKeyCopied");
        }

        private void ExportSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            ExportKey(() => _keyExportService.ExportPrivateKey(entry, _appSettingsService.GetExportFolderPath()));
        }

        private async void DeleteSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            ContentDialogResult dialogResult = await ShowConfirmDialogAsync(
                "DeletePrivateKeyDialogTitle",
                "DeleteKeyDialogPrimaryButtonText",
                string.Format(AppResources.GetString("DeleteKeyDialogContent"), entry.DisplayName));

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            _privateKeys.Remove(entry);
            SelectFirstPrivateKeyIfAvailable();
            if (SaveKeyStore())
            {
                ShowStatus("StatusPrivateKeyDeleted", InfoBarSeverity.Success);
            }
        }

        private void SavePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SavePrivateKeyPasswordAsync();
        }

        private async System.Threading.Tasks.Task SavePrivateKeyPasswordAsync()
        {
            PasswordBox passwordBox = new()
            {
                PlaceholderText = AppResources.GetString("PrivateKeyPasswordBox.PlaceholderText"),
                MinWidth = 320
            };

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "SavePrivateKeyPasswordDialogTitle",
                "SavePrivateKeyPasswordDialogPrimaryButtonText",
                passwordBox);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            string password = passwordBox.Password;
            passwordBox.Password = string.Empty;

            SavePrivateKeyPassword(password);
        }

        private void SavePrivateKeyPassword(string password)
        {
            try
            {
                _credentialManagerService.SavePrivateKeyPassword(password);
                ShowStatus("StatusPrivateKeyPasswordSaved", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private async void DeletePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult dialogResult = await ShowConfirmDialogAsync(
                "DeletePrivateKeyPasswordDialogTitle",
                "DeleteKeyDialogPrimaryButtonText",
                AppResources.GetString("DeletePrivateKeyPasswordDialogContent"));

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            DeletePrivateKeyPassword();
        }

        private void DeletePrivateKeyPassword()
        {
            try
            {
                _credentialManagerService.DeletePrivateKeyPassword();
                ShowStatus("StatusPrivateKeyPasswordDeleted", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private async void ChooseExportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            try
            {
                _appSettingsService.SetExportFolderPath(folder.Path);
                SettingsView.ExportFolderPath = folder.Path;
                ShowStatus("StatusExportFolderSaved", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task ImportRecipientPublicKeyFromFileAsync(TextBox publicKeyTextBox)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".pub");
            picker.FileTypeFilter.Add(".pem");
            picker.FileTypeFilter.Add(".txt");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            try
            {
                publicKeyTextBox.Text = await File.ReadAllTextAsync(file.Path, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowStatus("ErrorPublicKeyFileReadFailed", InfoBarSeverity.Error);
            }
        }

        private void OpenExportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _keyExportService.OpenFolder(_appSettingsService.GetExportFolderPath());
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
                    DecryptView.CipherText = await content.GetTextAsync();
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

        private void LoadKeyStore()
        {
            try
            {
                KeyStoreData data = _keyStoreService.Load();
                foreach (KeyEntry entry in data.RecipientKeys)
                {
                    _recipientKeys.Add(entry);
                }

                foreach (KeyEntry entry in data.PrivateKeys)
                {
                    _privateKeys.Add(entry);
                }

                if (_recipientKeys.Count > 0)
                {
                    SelectFirstRecipientKeyIfAvailable();
                }

                if (_privateKeys.Count > 0)
                {
                    SelectFirstPrivateKeyIfAvailable();
                }
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                SettingsView.ExportFolderPath = _appSettingsService.GetExportFolderPath();
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private bool SaveKeyStore()
        {
            try
            {
                _keyStoreService.Save(_recipientKeys, _privateKeys);
                return true;
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
                return false;
            }
        }

        private void ExportKey(Func<string> exportAction)
        {
            try
            {
                exportAction();
                ShowStatus("StatusKeyExported", InfoBarSeverity.Success);
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private void SelectFirstRecipientKeyIfAvailable()
        {
            int selectedIndex = _recipientKeys.Count > 0 ? 0 : -1;
            EncryptView.SelectedRecipientIndex = selectedIndex;
            RecipientKeysView.SelectedIndex = selectedIndex;
        }

        private void SelectFirstPrivateKeyIfAvailable()
        {
            int selectedIndex = _privateKeys.Count > 0 ? 0 : -1;
            DecryptView.SelectedPrivateKeyIndex = selectedIndex;
            PrivateKeysView.SelectedIndex = selectedIndex;
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

        private async System.Threading.Tasks.Task<ContentDialogResult> ShowConfirmDialogAsync(
            string titleResourceKey,
            string primaryButtonResourceKey,
            string content)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootNavigation.XamlRoot,
                Title = AppResources.GetString(titleResourceKey),
                PrimaryButtonText = AppResources.GetString(primaryButtonResourceKey),
                CloseButtonText = AppResources.GetString("DialogCancelButtonText"),
                DefaultButton = ContentDialogButton.Close,
                Content = content
            };

            return await dialog.ShowAsync();
        }
    }
}
