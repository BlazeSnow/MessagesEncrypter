using MessagesEncrypter.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace MessagesEncrypter.Services;

public sealed class KeyStoreService
{
    private const string DatabaseFileName = "keys.db";
    private const string LegacyStoreFileName = "keys.json";
    private const string LegacyMigratedStoreFileName = "keys.json.migrated";
    private const string RecipientKeyCategory = "recipient";
    private const string PrivateKeyCategory = "private";

    private static readonly AppJsonSerializerContext JsonContext = new(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    });

    public string StorePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);

    private string LegacyStorePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, LegacyStoreFileName);

    private string LegacyMigratedStorePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, LegacyMigratedStoreFileName);

    private KeyStoreIntegrityService IntegrityService => new(ApplicationData.Current.LocalFolder.Path);

    public KeyStoreData Load(bool trustCurrentStore = false)
    {
        try
        {
            Directory.CreateDirectory(ApplicationData.Current.LocalFolder.Path);
            bool hadDatabase = File.Exists(StorePath);
            if (hadDatabase && !trustCurrentStore)
            {
                IntegrityService.VerifyFile(StorePath);
            }

            EnsureDatabase();
            bool migrated = MigrateLegacyJsonIfNeeded();
            if (migrated || !hadDatabase || trustCurrentStore)
            {
                IntegrityService.SignFile(StorePath, trustCurrentStore);
            }

            using SqliteConnection connection = OpenConnection();
            var data = new KeyStoreData();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT category, alias, fingerprint, public_key_pem, encrypted_private_key_pem
                FROM keys
                ORDER BY category, alias COLLATE NOCASE, fingerprint COLLATE NOCASE;
                """;

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string category = reader.GetString(0);
                var entry = new KeyEntry(
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4));

                if (category == RecipientKeyCategory)
                {
                    data.RecipientKeys.Add(entry);
                }
                else if (category == PrivateKeyCategory)
                {
                    data.PrivateKeys.Add(entry);
                }
            }

            return data;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or SqliteException or CryptographicException)
        {
            throw new CryptoException("ErrorKeyStoreLoadFailed", ex);
        }
    }

    public void Save(IEnumerable<KeyEntry> recipientKeys, IEnumerable<KeyEntry> privateKeys)
    {
        try
        {
            EnsureDatabase();

            using (SqliteConnection connection = OpenConnection())
            {
                using SqliteTransaction transaction = connection.BeginTransaction();
                using SqliteCommand deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM keys;";
                deleteCommand.ExecuteNonQuery();

                InsertKeys(connection, transaction, RecipientKeyCategory, recipientKeys);
                InsertKeys(connection, transaction, PrivateKeyCategory, privateKeys);

                transaction.Commit();
            }

            IntegrityService.SignFile(StorePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException or CryptographicException)
        {
            throw new CryptoException("ErrorKeyStoreSaveFailed", ex);
        }
    }

    private void EnsureDatabase()
    {
        Directory.CreateDirectory(ApplicationData.Current.LocalFolder.Path);

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS keys (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                alias TEXT NOT NULL,
                fingerprint TEXT NOT NULL,
                public_key_pem TEXT NULL,
                encrypted_private_key_pem TEXT NULL
            );
            DELETE FROM keys
            WHERE id NOT IN (
                SELECT MIN(id)
                FROM keys
                GROUP BY category, fingerprint
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_keys_category_fingerprint
            ON keys (category, fingerprint);
            """;
        command.ExecuteNonQuery();
    }

    private bool MigrateLegacyJsonIfNeeded()
    {
        if (!File.Exists(LegacyStorePath) || HasStoredKeys())
        {
            return false;
        }

        string json = File.ReadAllText(LegacyStorePath, Encoding.UTF8);
        KeyStoreData data = JsonSerializer.Deserialize(json, JsonContext.KeyStoreData) ?? new KeyStoreData();
        Save(data.RecipientKeys, data.PrivateKeys);
        File.Move(LegacyStorePath, LegacyMigratedStorePath, true);
        return true;
    }

    private bool HasStoredKeys()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM keys LIMIT 1);";
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    private static void InsertKeys(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string category,
        IEnumerable<KeyEntry> keys)
    {
        int sortOrder = 0;
        foreach (KeyEntry key in keys)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO keys (
                    category,
                    sort_order,
                    alias,
                    fingerprint,
                    public_key_pem,
                    encrypted_private_key_pem
                )
                VALUES (
                    $category,
                    $sortOrder,
                    $alias,
                    $fingerprint,
                    $publicKeyPem,
                    $encryptedPrivateKeyPem
                );
                """;
            command.Parameters.AddWithValue("$category", category);
            command.Parameters.AddWithValue("$sortOrder", sortOrder);
            command.Parameters.AddWithValue("$alias", key.Alias);
            command.Parameters.AddWithValue("$fingerprint", key.Fingerprint);
            command.Parameters.AddWithValue("$publicKeyPem", (object?)key.PublicKeyPem ?? DBNull.Value);
            command.Parameters.AddWithValue("$encryptedPrivateKeyPem", (object?)key.EncryptedPrivateKeyPem ?? DBNull.Value);
            command.ExecuteNonQuery();
            sortOrder++;
        }
    }

    private SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = StorePath
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }
}
