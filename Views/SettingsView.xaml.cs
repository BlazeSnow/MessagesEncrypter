using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MessagesEncrypter.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? ChooseExportFolderRequested;

    public event RoutedEventHandler? OpenExportFolderRequested;

    public event RoutedEventHandler? SavePrivateKeyPasswordRequested;

    public event RoutedEventHandler? DeletePrivateKeyPasswordRequested;

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

    private void SavePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        SavePrivateKeyPasswordRequested?.Invoke(sender, e);
    }

    private void DeletePrivateKeyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        DeletePrivateKeyPasswordRequested?.Invoke(sender, e);
    }
}
