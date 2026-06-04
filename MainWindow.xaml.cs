using MessagesEncrypter.Models;
using MessagesEncrypter.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
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
        private string? _selectedRecipientKeyFingerprint;
        private string? _selectedPrivateKeyFingerprint;
        private readonly DispatcherTimer _statusDismissTimer = new()
        {
            Interval = TimeSpan.FromSeconds(8)
        };

        public MainWindow()
        {
            _messageCryptoService = new MessageCryptoService(_keyManagementService);
            InitializeComponent();
            _statusDismissTimer.Tick += StatusDismissTimer_Tick;
            Title = AppResources.GetString("MainWindowTitle");
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            InitializeViews();
            LoadKeyStore();
            LoadSettings();
            ShowPanel("Home");
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                ShowPanel(tag);
            }
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
        }

        private void ShowPanel(string tag)
        {
            RestoreKeySelectionForPanel(tag);

            HomeView.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            EncryptView.Visibility = tag == "Encrypt" ? Visibility.Visible : Visibility.Collapsed;
            DecryptView.Visibility = tag == "Decrypt" ? Visibility.Visible : Visibility.Collapsed;
            RecipientKeysView.Visibility = tag == "RecipientKeys" ? Visibility.Visible : Visibility.Collapsed;
            PrivateKeysView.Visibility = tag == "PrivateKeys" ? Visibility.Visible : Visibility.Collapsed;
            SettingsView.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitializeViews()
        {
            EncryptView.RecipientKeysSource = _recipientKeys;
            EncryptView.EncryptRequested += EncryptButton_Click;
            EncryptView.CopyEncryptedMessageRequested += CopyEncryptedMessageButton_Click;
            EncryptView.SelectedRecipientKeyChanged += (_, entry) => _selectedRecipientKeyFingerprint = entry?.Fingerprint;

            DecryptView.PrivateKeysSource = _privateKeys;
            DecryptView.DecryptRequested += DecryptButton_Click;
            DecryptView.PasteEncryptedMessageRequested += PasteEncryptedMessageButton_Click;
            DecryptView.SelectedPrivateKeyChanged += (_, entry) => _selectedPrivateKeyFingerprint = entry?.Fingerprint;

            RecipientKeysView.ItemsSource = _recipientKeys;
            RecipientKeysView.ImportRequested += ImportRecipientKeyButton_Click;
            RecipientKeysView.CopyRequested += CopySelectedRecipientKeyButton_Click;
            RecipientKeysView.ExportRequested += ExportSelectedRecipientKeyButton_Click;
            RecipientKeysView.RenameRequested += RenameSelectedRecipientKeyButton_Click;
            RecipientKeysView.OpenExportFolderRequested += OpenExportFolderButton_Click;
            RecipientKeysView.DeleteRequested += DeleteSelectedRecipientKeyButton_Click;

            PrivateKeysView.ItemsSource = _privateKeys;
            PrivateKeysView.GenerateRequested += GenerateKeyButton_Click;
            PrivateKeysView.ImportRequested += ImportPrivateKeyButton_Click;
            PrivateKeysView.CopyPublicKeyRequested += CopySelectedPrivatePublicKeyButton_Click;
            PrivateKeysView.ExportPublicKeyRequested += ExportSelectedPrivatePublicKeyButton_Click;
            PrivateKeysView.CopyPrivateKeyRequested += CopySelectedPrivateKeyButton_Click;
            PrivateKeysView.ExportPrivateKeyRequested += ExportSelectedPrivateKeyButton_Click;
            PrivateKeysView.ChangePasswordRequested += ChangePrivateKeyPasswordButton_Click;
            PrivateKeysView.RenameRequested += RenameSelectedPrivateKeyButton_Click;
            PrivateKeysView.OpenExportFolderRequested += OpenExportFolderButton_Click;
            PrivateKeysView.DeleteRequested += DeleteSelectedPrivateKeyButton_Click;

            SettingsView.ChooseExportFolderRequested += ChooseExportFolderButton_Click;
            SettingsView.OpenExportFolderRequested += OpenExportFolderButton_Click;
        }

        private async void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox aliasTextBox = CreateDialogTextBox("PrivateKeyAliasTextBox");
            ComboBox keySizeComboBox = CreateRsaKeySizeComboBox();
            PasswordBox passwordBox = CreateDialogPasswordBox("PrivateKeyPasswordBox");
            PasswordBox confirmPasswordBox = CreateDialogPasswordBox("PrivateKeyPasswordConfirmBox");
            CheckBox rememberPasswordCheckBox = CreateDialogCheckBox("RememberPrivateKeyPasswordCheckBox");
            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(aliasTextBox);
            dialogContent.Children.Add(keySizeComboBox);
            dialogContent.Children.Add(passwordBox);
            dialogContent.Children.Add(confirmPasswordBox);
            dialogContent.Children.Add(rememberPasswordCheckBox);

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "GeneratePrivateKeyDialogTitle",
                "GeneratePrivateKeyDialogPrimaryButtonText",
                dialogContent);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            string password = passwordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;
            passwordBox.Password = string.Empty;
            confirmPasswordBox.Password = string.Empty;

            if (password != confirmPassword)
            {
                ShowStatus("ErrorPasswordConfirmMismatch", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                int keySizeBits = keySizeComboBox.SelectedItem is int selectedKeySize
                    ? selectedKeySize
                    : CryptoConstants.DefaultRsaKeySizeBits;
                ShowOperationProgress(true);
                KeyPairResult result = await System.Threading.Tasks.Task.Run(() =>
                    _keyManagementService.GenerateKeyPair(password, keySizeBits));
                string alias = GetAliasOrDefault(aliasTextBox.Text, "DefaultPrivateKeyAlias", _privateKeys.Count + 1);
                var entry = new KeyEntry(alias, result.PublicKeyFingerprint, result.PublicKeyPem, result.EncryptedPrivateKeyPem);
                if (rememberPasswordCheckBox.IsChecked == true)
                {
                    _credentialManagerService.SavePrivateKeyPassword(entry.Fingerprint, password);
                }

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
            finally
            {
                ShowOperationProgress(false);
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

        private async void ImportPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox aliasTextBox = CreateDialogTextBox("PrivateKeyAliasTextBox");
            PasswordBox passwordBox = CreateDialogPasswordBox("PrivateKeyPasswordBox");
            CheckBox rememberPasswordCheckBox = CreateDialogCheckBox("RememberPrivateKeyPasswordCheckBox");
            TextBox privateKeyTextBox = CreateDialogTextBox("PrivateKeyTextBox");
            privateKeyTextBox.AcceptsReturn = true;
            privateKeyTextBox.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
            privateKeyTextBox.Height = 180;
            privateKeyTextBox.TextWrapping = TextWrapping.NoWrap;
            Button importFromFileButton = new()
            {
                Content = CreateIconTextContent("\uE8B5", AppResources.GetString("ImportPrivateKeyFromFileButtonText"))
            };
            importFromFileButton.Click += async (_, _) => await ImportPrivateKeyFromFileAsync(privateKeyTextBox);

            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(aliasTextBox);
            dialogContent.Children.Add(passwordBox);
            dialogContent.Children.Add(rememberPasswordCheckBox);
            dialogContent.Children.Add(importFromFileButton);
            dialogContent.Children.Add(privateKeyTextBox);

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "ImportPrivateKeyDialogTitle",
                "ImportPrivateKeyDialogPrimaryButtonText",
                dialogContent);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            string password = passwordBox.Password;
            passwordBox.Password = string.Empty;

            try
            {
                KeyPairResult result = _keyManagementService.ImportKeyPair(privateKeyTextBox.Text, password);
                string alias = GetAliasOrDefault(aliasTextBox.Text, "DefaultPrivateKeyAlias", _privateKeys.Count + 1);
                var entry = new KeyEntry(alias, result.PublicKeyFingerprint, result.PublicKeyPem, result.EncryptedPrivateKeyPem);
                if (rememberPasswordCheckBox.IsChecked == true)
                {
                    _credentialManagerService.SavePrivateKeyPassword(entry.Fingerprint, password);
                }

                _privateKeys.Add(entry);
                DecryptView.SelectPrivateKey(entry);
                PrivateKeysView.SelectKey(entry);
                if (SaveKeyStore())
                {
                    ShowStatus("StatusPrivateKeyImported", InfoBarSeverity.Success);
                }
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
        }

        private async void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DecryptView.SelectedPrivateKey is not KeyEntry privateKey || string.IsNullOrWhiteSpace(privateKey.EncryptedPrivateKeyPem))
                {
                    ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                PrivateKeyPasswordResult passwordResult = await GetPasswordForPrivateKeyAsync(privateKey);
                if (string.IsNullOrEmpty(passwordResult.Password))
                {
                    return;
                }

                DecryptView.DecryptedMessage = _messageCryptoService.DecryptFromBase64Json(
                    DecryptView.CipherText,
                    privateKey.EncryptedPrivateKeyPem,
                    passwordResult.Password);
                if (passwordResult.ShouldSave)
                {
                    _credentialManagerService.SavePrivateKeyPassword(privateKey.Fingerprint, passwordResult.Password);
                }

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
                Content = CreateIconTextContent("\uE8B5", AppResources.GetString("ImportRecipientKeyFromFileButtonText"))
            };
            importFromFileButton.Click += async (_, _) => await ImportRecipientPublicKeyFromFileAsync(aliasTextBox, publicKeyTextBox);

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

        private async void RenameSelectedRecipientKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            string? alias = await PromptKeyAliasAsync(entry.Alias);
            if (alias is null)
            {
                return;
            }

            var updatedEntry = new KeyEntry(alias, entry.Fingerprint, entry.PublicKeyPem, entry.EncryptedPrivateKeyPem);
            int index = _recipientKeys.IndexOf(entry);
            if (index < 0)
            {
                ShowStatus("ErrorRecipientKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            _recipientKeys[index] = updatedEntry;
            RecipientKeysView.SelectKey(updatedEntry);
            EncryptView.SelectRecipientKey(updatedEntry);
            if (SaveKeyStore())
            {
                ShowStatus("StatusKeyAliasChanged", InfoBarSeverity.Success);
            }
            else
            {
                _recipientKeys[index] = entry;
                RecipientKeysView.SelectKey(entry);
                EncryptView.SelectRecipientKey(entry);
            }
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

        private async void RenameSelectedPrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            string? alias = await PromptKeyAliasAsync(entry.Alias);
            if (alias is null)
            {
                return;
            }

            var updatedEntry = new KeyEntry(alias, entry.Fingerprint, entry.PublicKeyPem, entry.EncryptedPrivateKeyPem);
            int index = _privateKeys.IndexOf(entry);
            if (index < 0)
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            _privateKeys[index] = updatedEntry;
            PrivateKeysView.SelectKey(updatedEntry);
            DecryptView.SelectPrivateKey(updatedEntry);
            if (SaveKeyStore())
            {
                ShowStatus("StatusKeyAliasChanged", InfoBarSeverity.Success);
            }
            else
            {
                _privateKeys[index] = entry;
                PrivateKeysView.SelectKey(entry);
                DecryptView.SelectPrivateKey(entry);
            }
        }

        private async void ChangePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrivateKeysView.SelectedKey is not KeyEntry entry || string.IsNullOrWhiteSpace(entry.EncryptedPrivateKeyPem))
            {
                ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                return;
            }

            PasswordBox oldPasswordBox = CreateDialogPasswordBox("OldPrivateKeyPasswordBox");
            PasswordBox newPasswordBox = CreateDialogPasswordBox("NewPrivateKeyPasswordBox");
            PasswordBox confirmPasswordBox = CreateDialogPasswordBox("NewPrivateKeyPasswordConfirmBox");
            CheckBox rememberPasswordCheckBox = CreateDialogCheckBox("RememberPrivateKeyPasswordCheckBox");
            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(oldPasswordBox);
            dialogContent.Children.Add(newPasswordBox);
            dialogContent.Children.Add(confirmPasswordBox);
            dialogContent.Children.Add(rememberPasswordCheckBox);

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "ChangePrivateKeyPasswordDialogTitle",
                "ChangePrivateKeyPasswordDialogPrimaryButtonText",
                dialogContent);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            string oldPassword = oldPasswordBox.Password;
            string newPassword = newPasswordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;
            oldPasswordBox.Password = string.Empty;
            newPasswordBox.Password = string.Empty;
            confirmPasswordBox.Password = string.Empty;

            if (newPassword != confirmPassword)
            {
                ShowStatus("ErrorPasswordConfirmMismatch", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                string encryptedPrivateKeyPem = _keyManagementService.ChangePrivateKeyPassword(
                    entry.EncryptedPrivateKeyPem,
                    oldPassword,
                    newPassword);
                var updatedEntry = new KeyEntry(
                    entry.Alias,
                    entry.Fingerprint,
                    entry.PublicKeyPem,
                    encryptedPrivateKeyPem);
                int index = _privateKeys.IndexOf(entry);
                if (index < 0)
                {
                    ShowStatus("ErrorPrivateKeyNotSelected", InfoBarSeverity.Warning);
                    return;
                }

                _privateKeys[index] = updatedEntry;
                DecryptView.SelectPrivateKey(updatedEntry);
                PrivateKeysView.SelectKey(updatedEntry);
                if (rememberPasswordCheckBox.IsChecked == true)
                {
                    _credentialManagerService.SavePrivateKeyPassword(updatedEntry.Fingerprint, newPassword);
                }
                else
                {
                    _credentialManagerService.DeletePrivateKeyPassword(updatedEntry.Fingerprint);
                }

                if (SaveKeyStore())
                {
                    ShowStatus("StatusPrivateKeyPasswordChanged", InfoBarSeverity.Success);
                }
            }
            catch (CryptoException ex)
            {
                ShowStatus(ex.ResourceKey, InfoBarSeverity.Error);
            }
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

            try
            {
                _privateKeys.Remove(entry);
                _credentialManagerService.DeletePrivateKeyPassword(entry.Fingerprint);
                SelectFirstPrivateKeyIfAvailable();
                if (SaveKeyStore())
                {
                    ShowStatus("StatusPrivateKeyDeleted", InfoBarSeverity.Success);
                }
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

        private async System.Threading.Tasks.Task ImportRecipientPublicKeyFromFileAsync(TextBox aliasTextBox, TextBox publicKeyTextBox)
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
                if (string.IsNullOrWhiteSpace(aliasTextBox.Text))
                {
                    aliasTextBox.Text = Path.GetFileNameWithoutExtension(file.Name);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowStatus("ErrorPublicKeyFileReadFailed", InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task ImportPrivateKeyFromFileAsync(TextBox privateKeyTextBox)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".pem");
            picker.FileTypeFilter.Add(".key");
            picker.FileTypeFilter.Add(".txt");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            try
            {
                privateKeyTextBox.Text = await File.ReadAllTextAsync(file.Path, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowStatus("ErrorPrivateKeyFileReadFailed", InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task<PrivateKeyPasswordResult> GetPasswordForPrivateKeyAsync(KeyEntry privateKey)
        {
            if (_credentialManagerService.HasPrivateKeyPassword(privateKey.Fingerprint))
            {
                return new PrivateKeyPasswordResult(
                    _credentialManagerService.GetPrivateKeyPassword(privateKey.Fingerprint),
                    false);
            }

            PasswordBox passwordBox = CreateDialogPasswordBox("PrivateKeyPasswordBox");
            CheckBox rememberPasswordCheckBox = CreateDialogCheckBox("RememberPrivateKeyPasswordCheckBox");
            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(passwordBox);
            dialogContent.Children.Add(rememberPasswordCheckBox);

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "UnlockPrivateKeyDialogTitle",
                "UnlockPrivateKeyDialogPrimaryButtonText",
                dialogContent);

            if (dialogResult != ContentDialogResult.Primary)
            {
                passwordBox.Password = string.Empty;
                return new PrivateKeyPasswordResult(string.Empty, false);
            }

            string password = passwordBox.Password;
            passwordBox.Password = string.Empty;
            return new PrivateKeyPasswordResult(password, rememberPasswordCheckBox.IsChecked == true);
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
            _statusDismissTimer.Stop();
            StatusInfoBar.Message = AppResources.GetString(resourceKey);
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Visibility = Visibility.Visible;
            StatusInfoBar.IsOpen = true;
            _statusDismissTimer.Start();
        }

        private void StatusInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            _statusDismissTimer.Stop();
            StatusInfoBar.Visibility = Visibility.Collapsed;
        }

        private void StatusDismissTimer_Tick(object? sender, object e)
        {
            _statusDismissTimer.Stop();
            StatusInfoBar.IsOpen = false;
            StatusInfoBar.Visibility = Visibility.Collapsed;
        }

        private void ShowOperationProgress(bool isVisible)
        {
            OperationProgressRing.IsActive = isVisible;
            OperationProgressRing.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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
                string exportedFilePath = exportAction();
                _keyExportService.SelectFile(exportedFilePath);
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

        private void RestoreKeySelectionForPanel(string tag)
        {
            if (tag == "Encrypt")
            {
                RestoreRecipientKeySelection();
            }
            else if (tag == "Decrypt")
            {
                RestorePrivateKeySelection();
            }
        }

        private void RestoreRecipientKeySelection()
        {
            if (TrySelectKeyByFingerprint(_recipientKeys, _selectedRecipientKeyFingerprint, EncryptView.SelectRecipientKey))
            {
                return;
            }

            if (EncryptView.SelectedRecipientKey is null)
            {
                EncryptView.SelectedRecipientIndex = _recipientKeys.Count > 0 ? 0 : -1;
            }
        }

        private void RestorePrivateKeySelection()
        {
            if (TrySelectKeyByFingerprint(_privateKeys, _selectedPrivateKeyFingerprint, DecryptView.SelectPrivateKey))
            {
                return;
            }

            if (DecryptView.SelectedPrivateKey is null)
            {
                DecryptView.SelectedPrivateKeyIndex = _privateKeys.Count > 0 ? 0 : -1;
            }
        }

        private static bool TrySelectKeyByFingerprint(
            ObservableCollection<KeyEntry> keys,
            string? fingerprint,
            Action<KeyEntry> selectKey)
        {
            if (string.IsNullOrEmpty(fingerprint))
            {
                return false;
            }

            foreach (KeyEntry key in keys)
            {
                if (key.Fingerprint == fingerprint)
                {
                    selectKey(key);
                    return true;
                }
            }

            return false;
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
                RequestedTheme = RootNavigation.ActualTheme,
                PlaceholderText = AppResources.GetString($"{resourcePrefix}.PlaceholderText")
            };
        }

        private PasswordBox CreateDialogPasswordBox(string resourcePrefix)
        {
            return new PasswordBox
            {
                RequestedTheme = RootNavigation.ActualTheme,
                MinWidth = 320,
                PlaceholderText = AppResources.GetString($"{resourcePrefix}.PlaceholderText")
            };
        }

        private CheckBox CreateDialogCheckBox(string resourcePrefix)
        {
            return new CheckBox
            {
                RequestedTheme = RootNavigation.ActualTheme,
                Content = AppResources.GetString($"{resourcePrefix}.Content")
            };
        }

        private ComboBox CreateRsaKeySizeComboBox()
        {
            var comboBox = new ComboBox
            {
                RequestedTheme = RootNavigation.ActualTheme,
                MinWidth = 320,
                Header = AppResources.GetString("RsaKeySizeComboBox.Header")
            };

            foreach (int keySizeBits in CryptoConstants.SupportedRsaKeySizesBits)
            {
                comboBox.Items.Add(keySizeBits);
            }

            comboBox.SelectedItem = CryptoConstants.DefaultRsaKeySizeBits;
            return comboBox;
        }

        private async System.Threading.Tasks.Task<string?> PromptKeyAliasAsync(string currentAlias)
        {
            TextBox aliasTextBox = CreateDialogTextBox("RenameKeyAliasTextBox");
            aliasTextBox.Text = currentAlias;

            ContentDialogResult dialogResult = await ShowInputDialogAsync(
                "RenameKeyDialogTitle",
                "RenameKeyDialogPrimaryButtonText",
                aliasTextBox);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return null;
            }

            string alias = aliasTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(alias))
            {
                ShowStatus("ErrorKeyAliasRequired", InfoBarSeverity.Warning);
                return null;
            }

            return alias;
        }

        private static StackPanel CreateIconTextContent(string glyph, string text)
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };
            content.Children.Add(new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = glyph
            });
            content.Children.Add(new TextBlock
            {
                Text = text
            });
            return content;
        }

        private async System.Threading.Tasks.Task<ContentDialogResult> ShowInputDialogAsync(
            string titleResourceKey,
            string primaryButtonResourceKey,
            object content)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootNavigation.XamlRoot,
                RequestedTheme = RootNavigation.ActualTheme,
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
                RequestedTheme = RootNavigation.ActualTheme,
                Title = AppResources.GetString(titleResourceKey),
                PrimaryButtonText = AppResources.GetString(primaryButtonResourceKey),
                CloseButtonText = AppResources.GetString("DialogCancelButtonText"),
                DefaultButton = ContentDialogButton.Close,
                Content = content
            };

            return await dialog.ShowAsync();
        }

        private sealed record PrivateKeyPasswordResult(string Password, bool ShouldSave);
    }
}
