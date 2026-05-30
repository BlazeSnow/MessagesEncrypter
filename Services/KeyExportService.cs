using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MessagesEncrypter.Models;

namespace MessagesEncrypter.Services;

public sealed class KeyExportService
{
    public string ExportPublicKey(KeyEntry entry, string exportFolderPath)
    {
        if (string.IsNullOrWhiteSpace(entry.PublicKeyPem))
        {
            throw new CryptoException("ErrorPublicKeyRequired");
        }

        return ExportText(entry.PublicKeyPem, entry, exportFolderPath, ".pub");
    }

    public string ExportPrivateKey(KeyEntry entry, string exportFolderPath)
    {
        if (string.IsNullOrWhiteSpace(entry.EncryptedPrivateKeyPem))
        {
            throw new CryptoException("ErrorPrivateKeyRequired");
        }

        return ExportText(entry.EncryptedPrivateKeyPem, entry, exportFolderPath, ".pem");
    }

    public void OpenFolder(string folderPath)
    {
        try
        {
            Directory.CreateDirectory(folderPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            throw new CryptoException("ErrorExportFolderOpenFailed", ex);
        }
    }

    private static string ExportText(string text, KeyEntry entry, string exportFolderPath, string extension)
    {
        try
        {
            Directory.CreateDirectory(exportFolderPath);
            string fileName = $"{SanitizeFileName(entry.Alias)}-{entry.Fingerprint}{extension}";
            string path = Path.Combine(exportFolderPath, fileName);
            File.WriteAllText(path, text, Encoding.UTF8);
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new CryptoException("ErrorKeyExportFailed", ex);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        string sanitized = fileName.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "key" : sanitized;
    }
}
