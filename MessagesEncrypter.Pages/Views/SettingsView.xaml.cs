using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel;
using Windows.System;

namespace MessagesEncrypter.Pages.Views;

public sealed partial class SettingsView : UserControl
{
    private bool _isInitializingDisplayLanguage;

    public SettingsView()
    {
        InitializeComponent();
        AppVersionTextBlock.Text = GetAppVersion();
    }

    public event RoutedEventHandler? ChooseExportFolderRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event EventHandler<string>? DisplayLanguagePreferenceChanged;

    public string DisplayLanguagePreference
    {
        set => SelectDisplayLanguage(value);
    }

    public string ExportFolderPath
    {
        get => ExportFolderPathTextBlock.Text;
        set => ExportFolderPathTextBlock.Text = value;
    }

    private void ChooseExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        ChooseExportFolderRequested?.Invoke(sender, e);
    }

    private void OpenExportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExportFolderRequested?.Invoke(sender, e);
    }

    private void DisplayLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingDisplayLanguage || DisplayLanguageComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        DisplayLanguagePreferenceChanged?.Invoke(this, item.Tag as string ?? string.Empty);
    }

    private void SelectDisplayLanguage(string preference)
    {
        _isInitializingDisplayLanguage = true;
        try
        {
            DisplayLanguageComboBox.SelectedIndex = preference switch
            {
                "zh-Hans" => 1,
                "en-US" => 2,
                _ => 0
            };
        }
        finally
        {
            _isInitializingDisplayLanguage = false;
        }
    }

    private static string GetAppVersion()
    {
        PackageVersion version = Package.Current.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private async void OpenProjectRepositoryButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/BlazeSnow/MessagesEncrypter"));
    }

    private async void OpenProjectWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://www.blazesnow.com/messages/"));
    }

}
