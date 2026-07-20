using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel;
using Windows.System;

namespace MessagesEncrypter.Pages.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        AppVersionTextBlock.Text = GetAppVersion();
    }

    public event RoutedEventHandler? ChooseExportFolderRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event RoutedEventHandler? CopyFeedbackEmailRequested;

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

    private void CopyFeedbackEmailButton_Click(object sender, RoutedEventArgs e)
    {
        CopyFeedbackEmailRequested?.Invoke(sender, e);
    }
}
