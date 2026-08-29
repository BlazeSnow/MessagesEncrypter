using System;
using System.IO;
using MessagesEncrypter.Core.Models;
using MessagesEncrypter.Core.Services;
using Xunit;

namespace MessagesEncrypter.Tests;

/// <summary>
/// 密钥导出服务测试，覆盖导出文件内容、文件名净化和路径处理。
/// 不测试 OpenFolder/SelectFile，它们会启动资源管理器。
/// </summary>
public sealed class KeyExportServiceTests : IDisposable
{
    private const string PublicPem = "-----BEGIN PUBLIC KEY-----\nTEST\n-----END PUBLIC KEY-----";
    private const string PrivatePem = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nTEST\n-----END PRIVATE KEY-----";

    private readonly KeyExportService _service = new();
    private readonly string _exportFolder;

    public KeyExportServiceTests()
    {
        _exportFolder = Path.Combine(
            Path.GetTempPath(),
            "MessagesEncrypterTests",
            Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_exportFolder))
        {
            Directory.Delete(_exportFolder, recursive: true);
        }
    }

    [Fact]
    public void ExportPublicKey_WritesPemToPubFile()
    {
        var entry = new KeyEntry("别名", "AABB", PublicPem, null);

        string exportedPath = _service.ExportPublicKey(entry, _exportFolder);

        Assert.Equal(Path.Combine(_exportFolder, "别名.pub"), exportedPath);
        Assert.True(File.Exists(exportedPath));
        Assert.Equal(PublicPem, File.ReadAllText(exportedPath));
    }

    [Fact]
    public void ExportPrivateKey_WritesEncryptedPemToPemFile()
    {
        var entry = new KeyEntry("私钥", "CCDD", PublicPem, PrivatePem);

        string exportedPath = _service.ExportPrivateKey(entry, _exportFolder);

        Assert.Equal(Path.Combine(_exportFolder, "私钥.pem"), exportedPath);
        Assert.Equal(PrivatePem, File.ReadAllText(exportedPath));
    }

    [Fact]
    public void ExportPublicKey_MissingPublicKey_ThrowsPublicKeyRequired()
    {
        var entry = new KeyEntry("无公钥", "EEFF", null, PrivatePem);

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ExportPublicKey(entry, _exportFolder));
        Assert.Equal("ErrorPublicKeyRequired", exception.ResourceKey);
    }

    [Fact]
    public void ExportPrivateKey_MissingPrivateKey_ThrowsPrivateKeyRequired()
    {
        var entry = new KeyEntry("无私钥", "ABCD", PublicPem, null);

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ExportPrivateKey(entry, _exportFolder));
        Assert.Equal("ErrorPrivateKeyRequired", exception.ResourceKey);
    }

    [Theory]
    [InlineData("a/b\\c:d*e?f\"g<h>i|j")]
    [InlineData("名字<1>:带非法字符?")]
    public void ExportPublicKey_AliasWithIllegalFileNameChars_SanitizesFileName(string alias)
    {
        var entry = new KeyEntry(alias, "AABB", PublicPem, null);

        string exportedPath = _service.ExportPublicKey(entry, _exportFolder);

        string fileName = Path.GetFileName(exportedPath);
        Assert.DoesNotContain('/', fileName);
        Assert.DoesNotContain('\\', fileName);
        Assert.DoesNotContain(':', fileName);
        Assert.DoesNotContain('*', fileName);
        Assert.DoesNotContain('?', fileName);
        Assert.DoesNotContain('"', fileName);
        Assert.DoesNotContain('<', fileName);
        Assert.DoesNotContain('>', fileName);
        Assert.DoesNotContain('|', fileName);
        Assert.EndsWith(".pub", fileName, StringComparison.Ordinal);
        Assert.True(File.Exists(exportedPath));
    }

    [Fact]
    public void ExportPublicKey_WhitespaceAlias_UsesFallbackName()
    {
        var entry = new KeyEntry("   ", "AABB", PublicPem, null);

        string exportedPath = _service.ExportPublicKey(entry, _exportFolder);

        Assert.Equal("key.pub", Path.GetFileName(exportedPath));
        Assert.True(File.Exists(exportedPath));
    }

    [Fact]
    public void ExportPublicKey_NonExistentExportFolder_IsCreated()
    {
        string nestedFolder = Path.Combine(_exportFolder, "深一层", "再深一层");
        var entry = new KeyEntry("新目录", "AABB", PublicPem, null);

        string exportedPath = _service.ExportPublicKey(entry, nestedFolder);

        Assert.True(File.Exists(exportedPath));
    }

    [Fact]
    public void ExportPublicKey_ChineseExportFolder_Succeeds()
    {
        string chineseFolder = Path.Combine(_exportFolder, "导出文件夹", "密钥输出");
        var entry = new KeyEntry("中文别名", "AABB", PublicPem, null);

        string exportedPath = _service.ExportPublicKey(entry, chineseFolder);

        Assert.True(File.Exists(exportedPath));
        Assert.Equal(PublicPem, File.ReadAllText(exportedPath));
    }

    [Fact]
    public void ExportPublicKey_ExistingFile_IsOverwritten()
    {
        var firstEntry = new KeyEntry("同名", "AABB", PublicPem, null);
        var secondEntry = new KeyEntry("同名", "AABB", PublicPem.Replace("TEST", "REPLACED"), null);

        _service.ExportPublicKey(firstEntry, _exportFolder);
        string exportedPath = _service.ExportPublicKey(secondEntry, _exportFolder);

        Assert.Equal(PublicPem.Replace("TEST", "REPLACED"), File.ReadAllText(exportedPath));
    }
}
