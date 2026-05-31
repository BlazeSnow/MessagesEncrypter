using MessagesEncrypter.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace MessagesEncrypter.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? ChooseExportFolderRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

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

    private async void OpenProjectRepositoryButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/BlazeSnow/MessagesEncrypter"));
    }

    private async void OpenProjectWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://www.blazesnow.com/messages/"));
    }

    private async void CopyFeedbackEmailButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText("messages@blazesnow.com");
        Clipboard.SetContent(package);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Title = AppResources.GetString("FeedbackEmailCopiedDialogTitle"),
            Content = AppResources.GetString("FeedbackEmailCopiedDialogContent"),
            CloseButtonText = AppResources.GetString("DialogOkButtonText"),
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }
}
