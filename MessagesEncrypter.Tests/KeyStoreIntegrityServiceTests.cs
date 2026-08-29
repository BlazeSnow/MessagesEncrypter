using System;
using System.IO;
using System.Text;
using MessagesEncrypter.Core.Services;
using Xunit;

namespace MessagesEncrypter.Tests;

/// <summary>
/// 密钥库完整性校验测试。使用独立的凭据目标名，不触碰应用真实的完整性密钥。
/// </summary>
public sealed class KeyStoreIntegrityServiceTests : IDisposable
{
    private readonly string _integrityKeyTargetName =
        "MessagesEncrypter.KeyStoreIntegrityKey.UnitTests." + Guid.NewGuid().ToString("N");

    private readonly string _folderPath = Path.Combine(
        Path.GetTempPath(), "MessagesEncrypterTests", Guid.NewGuid().ToString("N"));

    private readonly KeyStoreIntegrityService _integrityService;
    private readonly string _storePath;

    public KeyStoreIntegrityServiceTests()
    {
        Directory.CreateDirectory(_folderPath);
        _integrityService = new KeyStoreIntegrityService(_folderPath, _integrityKeyTargetName);
        _storePath = Path.Combine(_folderPath, "keys.db");
    }

    public void Dispose()
    {
        _integrityService.DeleteIntegrityKey();
        if (Directory.Exists(_folderPath))
        {
            Directory.Delete(_folderPath, recursive: true);
        }
    }

    [Fact]
    public void SignThenVerify_NoError()
    {
        File.WriteAllText(_storePath, "数据库内容");

        _integrityService.SignFile(_storePath);

        Assert.Null(_integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void MissingStoreFile_NoError()
    {
        Assert.Null(_integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void MissingSignature_ReportsMissing()
    {
        File.WriteAllText(_storePath, "数据库内容");

        Assert.Equal("ErrorKeyStoreIntegrityMissing", _integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void TamperedStoreFile_ReportsInvalid()
    {
        File.WriteAllText(_storePath, "数据库内容");
        _integrityService.SignFile(_storePath);

        File.AppendAllText(_storePath, "被篡改的字节");

        Assert.Equal("ErrorKeyStoreIntegrityInvalid", _integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void GarbageSignatureContent_ReportsInvalid()
    {
        File.WriteAllText(_storePath, "数据库内容");
        File.WriteAllText(_integrityService.SignaturePath, "!!!不是 Base64!!!", Encoding.UTF8);

        Assert.Equal("ErrorKeyStoreIntegrityInvalid", _integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void ResignAfterModification_ClearsError()
    {
        File.WriteAllText(_storePath, "数据库内容");
        _integrityService.SignFile(_storePath);
        File.WriteAllText(_storePath, "修改后的数据库内容");

        Assert.Equal("ErrorKeyStoreIntegrityInvalid", _integrityService.GetIntegrityErrorResourceKey(_storePath));

        _integrityService.SignFile(_storePath, resetIntegrityKey: true);

        Assert.Null(_integrityService.GetIntegrityErrorResourceKey(_storePath));
    }

    [Fact]
    public void ResignWithoutResettingKey_AlsoClearsError()
    {
        File.WriteAllText(_storePath, "数据库内容");
        _integrityService.SignFile(_storePath);
        File.WriteAllText(_storePath, "修改后的数据库内容");

        _integrityService.SignFile(_storePath);

        Assert.Null(_integrityService.GetIntegrityErrorResourceKey(_storePath));
    }
}
