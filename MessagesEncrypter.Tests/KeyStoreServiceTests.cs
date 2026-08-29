using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MessagesEncrypter.Core.Models;
using MessagesEncrypter.Core.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MessagesEncrypter.Tests;

/// <summary>
/// 密钥库服务测试，覆盖存取往返、排序、重复指纹容忍、旧 JSON 迁移、旧数据库结构迁移和完整性签名。
/// 使用 internal 构造函数注入临时存储目录，不触碰应用真实数据。
/// 与 KeyStoreIntegrityServiceTests 共用集合串行执行：凭据管理器是机器级共享资源，并发调用会产生瞬时错误。
/// </summary>
[Collection("KeyStoreCredentialManager")]
public sealed class KeyStoreServiceTests : IDisposable
{
    private readonly string _integrityKeyTargetName =
        "MessagesEncrypter.KeyStoreIntegrityKey.UnitTests." + Guid.NewGuid().ToString("N");

    private readonly string _storeFolderPath = Path.Combine(
        Path.GetTempPath(), "MessagesEncrypterTests", Guid.NewGuid().ToString("N"));

    private readonly KeyStoreIntegrityService _integrityService;
    private readonly KeyStoreService _keyStoreService;

    public KeyStoreServiceTests()
    {
        Directory.CreateDirectory(_storeFolderPath);
        _integrityService = new KeyStoreIntegrityService(_storeFolderPath, _integrityKeyTargetName);
        _keyStoreService = new KeyStoreService(_storeFolderPath, _integrityKeyTargetName);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        _integrityService.DeleteIntegrityKey();
        if (Directory.Exists(_storeFolderPath))
        {
            Directory.Delete(_storeFolderPath, recursive: true);
        }
    }

    /// <summary>释放 SQLite 连接池持有的文件句柄，以便测试直接修改或删除 keys.db。</summary>
    private void ReleaseSqliteFileHandles()
    {
        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public void Load_EmptyFolder_CreatesStoreAndSignature()
    {
        KeyStoreData data = _keyStoreService.Load();

        Assert.Empty(data.RecipientKeys);
        Assert.Empty(data.PrivateKeys);
        Assert.True(File.Exists(_keyStoreService.StorePath));
        Assert.True(File.Exists(_integrityService.SignaturePath));
        Assert.Null(_integrityService.GetIntegrityErrorResourceKey(_keyStoreService.StorePath));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var recipient = new KeyEntry("接收方", "AA11", "公钥内容", null);
        var privateKey = new KeyEntry("私钥", "BB22", "公钥内容", "加密私钥内容");

        _keyStoreService.Save([recipient], [privateKey]);
        KeyStoreData loaded = _keyStoreService.Load();

        KeyEntry loadedRecipient = Assert.Single(loaded.RecipientKeys);
        Assert.Equal("接收方", loadedRecipient.Alias);
        Assert.Equal("AA11", loadedRecipient.Fingerprint);
        Assert.Equal("公钥内容", loadedRecipient.PublicKeyPem);
        Assert.Null(loadedRecipient.EncryptedPrivateKeyPem);

        KeyEntry loadedPrivateKey = Assert.Single(loaded.PrivateKeys);
        Assert.Equal("私钥", loadedPrivateKey.Alias);
        Assert.Equal("BB22", loadedPrivateKey.Fingerprint);
        Assert.Equal("公钥内容", loadedPrivateKey.PublicKeyPem);
        Assert.Equal("加密私钥内容", loadedPrivateKey.EncryptedPrivateKeyPem);
    }

    [Fact]
    public void Save_DuplicateFingerprint_DoesNotInterruptAndKeepsOne()
    {
        var first = new KeyEntry("别名一", "AA11", "公钥内容", null);
        var second = new KeyEntry("别名二", "AA11", "公钥内容", null);

        _keyStoreService.Save([first, second], []);
        KeyStoreData loaded = _keyStoreService.Load();

        KeyEntry entry = Assert.Single(loaded.RecipientKeys);
        Assert.Equal("AA11", entry.Fingerprint);
    }

    [Fact]
    public void Load_OrdersByAliasThenFingerprintCaseInsensitive()
    {
        _keyStoreService.Save(
        [
            new KeyEntry("b", "CC33", "公钥内容", null),
            new KeyEntry("B", "DD44", "公钥内容", null),
            new KeyEntry("same", "BB22", "公钥内容", null),
            new KeyEntry("same", "aa11", "公钥内容", null),
            new KeyEntry("A", "EE55", "公钥内容", null)
        ],
        []);

        var fingerprints = _keyStoreService.Load()
            .RecipientKeys
            .Select(entry => (entry.Alias, entry.Fingerprint))
            .ToList();

        Assert.Equal(
        [
            ("A", "EE55"),
            ("b", "CC33"),
            ("B", "DD44"),
            ("same", "aa11"),
            ("same", "BB22")
        ], fingerprints);
    }

    [Fact]
    public void Load_LegacyKeysJson_MigratesAndRenamesFile()
    {
        var legacyData = new KeyStoreData();
        legacyData.RecipientKeys.Add(new KeyEntry("旧接收方", "AA11", "旧公钥", null));
        legacyData.PrivateKeys.Add(new KeyEntry("旧私钥", "BB22", "旧公钥", "旧加密私钥"));
        string legacyPath = Path.Combine(_storeFolderPath, "keys.json");
        File.WriteAllText(
            legacyPath,
            JsonSerializer.Serialize(legacyData, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        KeyStoreData loaded = _keyStoreService.Load();

        KeyEntry recipient = Assert.Single(loaded.RecipientKeys);
        Assert.Equal("旧接收方", recipient.Alias);
        Assert.Equal("AA11", recipient.Fingerprint);
        KeyEntry privateKey = Assert.Single(loaded.PrivateKeys);
        Assert.Equal("BB22", privateKey.Fingerprint);
        Assert.False(File.Exists(legacyPath));
        Assert.True(File.Exists(Path.Combine(_storeFolderPath, "keys.json.migrated")));
    }

    [Fact]
    public void Load_LegacyKeysJson_WhenDatabaseHasKeys_IsSkipped()
    {
        var existing = new KeyEntry("库里已有", "AA11", "公钥内容", null);
        _keyStoreService.Save([existing], []);
        var legacyData = new KeyStoreData();
        legacyData.RecipientKeys.Add(new KeyEntry("旧数据", "BB22", "旧公钥", null));
        string legacyPath = Path.Combine(_storeFolderPath, "keys.json");
        File.WriteAllText(
            legacyPath,
            JsonSerializer.Serialize(legacyData, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        KeyStoreData loaded = _keyStoreService.Load();

        KeyEntry entry = Assert.Single(loaded.RecipientKeys);
        Assert.Equal("AA11", entry.Fingerprint);
        Assert.True(File.Exists(legacyPath));
        Assert.False(File.Exists(Path.Combine(_storeFolderPath, "keys.json.migrated")));
    }

    [Fact]
    public void GetIntegrityErrorResourceKey_TamperedStore_ReportsInvalid()
    {
        var entry = new KeyEntry("接收方", "AA11", "公钥内容", null);
        _keyStoreService.Save([entry], []);
        ReleaseSqliteFileHandles();
        File.AppendAllText(_keyStoreService.StorePath, "被篡改的字节");

        Assert.Equal("ErrorKeyStoreIntegrityInvalid", _keyStoreService.GetIntegrityErrorResourceKey());
    }

    [Fact]
    public void GetIntegrityErrorResourceKey_MissingSignature_ReportsMissing()
    {
        var entry = new KeyEntry("接收方", "AA11", "公钥内容", null);
        _keyStoreService.Save([entry], []);
        ReleaseSqliteFileHandles();
        File.Delete(_integrityService.SignaturePath);

        Assert.Equal("ErrorKeyStoreIntegrityMissing", _keyStoreService.GetIntegrityErrorResourceKey());
    }

    [Fact]
    public void Load_TrustCurrentStore_ResignsSoNextLoadPasses()
    {
        var entry = new KeyEntry("接收方", "AA11", "公钥内容", null);
        _keyStoreService.Save([entry], []);
        ReleaseSqliteFileHandles();
        File.AppendAllText(_keyStoreService.StorePath, "被篡改的字节");

        Assert.Equal("ErrorKeyStoreIntegrityInvalid", _keyStoreService.GetIntegrityErrorResourceKey());

        KeyStoreData trustedLoad = _keyStoreService.Load(trustCurrentStore: true);
        KeyEntry reloaded = Assert.Single(trustedLoad.RecipientKeys);
        Assert.Equal("AA11", reloaded.Fingerprint);

        Assert.Null(_keyStoreService.GetIntegrityErrorResourceKey());
        KeyStoreData normalLoad = _keyStoreService.Load();
        Assert.Single(normalLoad.RecipientKeys);
    }

    [Fact]
    public void Load_LegacySchemaWithIdColumn_MigratesAndDropsIdColumn()
    {
        CreateLegacySchemaStore();
        _integrityService.SignFile(_keyStoreService.StorePath);

        KeyStoreData loaded = _keyStoreService.Load();

        Assert.Equal(3, loaded.RecipientKeys.Count);
        Assert.Equal(1, loaded.RecipientKeys.Count(entry => entry.Fingerprint == "DD44"));

        using (SqliteConnection connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _keyStoreService.StorePath }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('keys') WHERE name = 'id';";
            Assert.Equal(0, Convert.ToInt32(command.ExecuteScalar()));
        }

        Assert.Null(_keyStoreService.GetIntegrityErrorResourceKey());
    }

    private void CreateLegacySchemaStore()
    {
        using SqliteConnection connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _keyStoreService.StorePath }.ToString());
        connection.Open();
        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = """
                CREATE TABLE keys (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    sort_order INTEGER NOT NULL,
                    alias TEXT NOT NULL,
                    fingerprint TEXT NOT NULL,
                    public_key_pem TEXT NULL,
                    encrypted_private_key_pem TEXT NULL
                );
                """;
            createCommand.ExecuteNonQuery();
        }

        using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.CommandText = """
                INSERT INTO keys (category, sort_order, alias, fingerprint, public_key_pem)
                VALUES
                    ('recipient', 0, '别名一', 'AA11', '公钥内容'),
                    ('recipient', 1, '别名二', 'BB22', '公钥内容'),
                    ('recipient', 2, '别名三', 'DD44', '公钥内容'),
                    ('recipient', 3, '别名三重复', 'DD44', '公钥内容');
                """;
            insertCommand.ExecuteNonQuery();
        }
    }
}
