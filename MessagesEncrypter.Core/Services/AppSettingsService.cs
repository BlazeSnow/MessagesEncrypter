using System;
using System.IO;
using Windows.Storage;

namespace MessagesEncrypter.Core.Services;

public sealed class AppSettingsService
{
    private const string ExportFolderPathKey = "ExportFolderPath";
    private const string DownloadsFolderName = "Downloads";

    public string GetExportFolderPath()
    {
        object? value = ApplicationData.Current.LocalSettings.Values[ExportFolderPathKey];
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DownloadsFolderName);
        SetExportFolderPath(defaultPath);
        return defaultPath;
    }

    public void SetExportFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CryptoException("ErrorExportFolderRequired");
        }

        ApplicationData.Current.LocalSettings.Values[ExportFolderPathKey] = path;
    }
}
