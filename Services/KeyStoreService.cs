using MessagesEncrypter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace MessagesEncrypter.Services;

public sealed class KeyStoreService
{
    private const string StoreFileName = "keys.json";

    private static readonly AppJsonSerializerContext JsonContext = new(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    });

    public string StorePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, StoreFileName);

    public KeyStoreData Load()
    {
        try
        {
            if (!File.Exists(StorePath))
            {
                return new KeyStoreData();
            }

            string json = File.ReadAllText(StorePath, Encoding.UTF8);
            return JsonSerializer.Deserialize(json, JsonContext.KeyStoreData) ?? new KeyStoreData();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new CryptoException("ErrorKeyStoreLoadFailed", ex);
        }
    }

    public void Save(IEnumerable<KeyEntry> recipientKeys, IEnumerable<KeyEntry> privateKeys)
    {
        try
        {
            Directory.CreateDirectory(ApplicationData.Current.LocalFolder.Path);

            var data = new KeyStoreData
            {
                RecipientKeys = [.. recipientKeys],
                PrivateKeys = [.. privateKeys]
            };

            string json = JsonSerializer.Serialize(data, JsonContext.KeyStoreData);
            File.WriteAllText(StorePath, json, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new CryptoException("ErrorKeyStoreSaveFailed", ex);
        }
    }
}
