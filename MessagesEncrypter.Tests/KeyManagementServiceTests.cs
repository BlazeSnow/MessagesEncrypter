using System;
using System.Linq;
using System.Security.Cryptography;
using MessagesEncrypter.Core.Services;
using Xunit;

namespace MessagesEncrypter.Tests;

/// <summary>
/// 密钥管理服务测试，覆盖密钥生成、导入、改密、指纹和输入校验。
/// </summary>
public sealed class KeyManagementServiceTests
{
    private const string TestPassword = "test-password-密码";

    private readonly KeyManagementService _service = new();

    [Fact]
    public void GenerateKeyPair_EmptyPassword_ThrowsPasswordRequired()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GenerateKeyPair("  ", 2048));
        Assert.Equal("ErrorPasswordRequired", exception.ResourceKey);
    }

    [Fact]
    public void GenerateKeyPair_UnsupportedKeySize_ThrowsRsaKeySizeUnsupported()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GenerateKeyPair(TestPassword, 1024));
        Assert.Equal("ErrorRsaKeySizeUnsupported", exception.ResourceKey);
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    [InlineData(4096)]
    [InlineData(8192)]
    public void GenerateKeyPair_EachSupportedSize_ProducesValidPemPair(int keySizeBits)
    {
        KeyPairResult result = _service.GenerateKeyPair(TestPassword, keySizeBits);

        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", result.PublicKeyPem, StringComparison.Ordinal);
        Assert.StartsWith("-----BEGIN ENCRYPTED PRIVATE KEY-----", result.EncryptedPrivateKeyPem, StringComparison.Ordinal);

        using RSA rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(result.EncryptedPrivateKeyPem, TestPassword);
        Assert.Equal(keySizeBits, rsa.KeySize);

        using RSA publicRsa = RSA.Create();
        publicRsa.ImportFromPem(result.PublicKeyPem);
        Assert.Equal(keySizeBits, publicRsa.KeySize);
    }

    [Fact]
    public void GenerateKeyPair_FingerprintMatchesPublicKey()
    {
        KeyPairResult result = _service.GenerateKeyPair(TestPassword, 2048);

        Assert.Equal(
            _service.GetPublicKeyFingerprint(result.PublicKeyPem),
            result.PublicKeyFingerprint);
    }

    [Fact]
    public void GenerateKeyPair_FingerprintIsStableHexFormat()
    {
        KeyPairResult result = _service.GenerateKeyPair(TestPassword, 2048);

        Assert.Equal(32, result.PublicKeyFingerprint.Length);
        Assert.True(
            result.PublicKeyFingerprint.All(char.IsAsciiHexDigit),
            $"指纹应只包含十六进制字符，实际为 {result.PublicKeyFingerprint}");
        Assert.Equal(
            _service.GetPublicKeyFingerprint(result.PublicKeyPem),
            result.PublicKeyFingerprint);
    }

    [Fact]
    public void GetPublicKeyFingerprint_SameKeyTwice_SameFingerprint()
    {
        using RSA rsa = RSA.Create(2048);
        string publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        Assert.Equal(
            _service.GetPublicKeyFingerprint(publicPem),
            _service.GetPublicKeyFingerprint(publicPem));
    }

    [Fact]
    public void GetPublicKeyFingerprint_DifferentKeys_DifferentFingerprints()
    {
        using RSA first = RSA.Create(2048);
        using RSA second = RSA.Create(2048);

        Assert.NotEqual(
            _service.GetPublicKeyFingerprint(first.ExportSubjectPublicKeyInfoPem()),
            _service.GetPublicKeyFingerprint(second.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void GetPublicKeyFingerprint_PrivateKeyPem_ThrowsPublicKeyInvalid()
    {
        using RSA rsa = RSA.Create(2048);
        string privatePem = rsa.ExportPkcs8PrivateKeyPem();

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GetPublicKeyFingerprint(privatePem));
        Assert.Equal("ErrorPublicKeyInvalid", exception.ResourceKey);
    }

    [Fact]
    public void GetPublicKeyFingerprint_Garbage_ThrowsPublicKeyInvalid()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GetPublicKeyFingerprint("这不是 PEM"));
        Assert.Equal("ErrorPublicKeyInvalid", exception.ResourceKey);
    }

    [Fact]
    public void GetPublicKeyFingerprint_Missing_ThrowsPublicKeyRequired()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GetPublicKeyFingerprint(" "));
        Assert.Equal("ErrorPublicKeyRequired", exception.ResourceKey);
    }

    [Fact]
    public void GetPublicKeyFingerprint_SmallPublicKey_ThrowsPublicKeyTooSmall()
    {
        using RSA rsa = RSA.Create(1024);
        string publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.GetPublicKeyFingerprint(publicPem));
        Assert.Equal("ErrorPublicKeyTooSmall", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_UnencryptedPkcs8PrivateKey_ReencryptsAndDerivesPublicKey()
    {
        using RSA original = RSA.Create(2048);
        string publicPem = original.ExportSubjectPublicKeyInfoPem();
        string privatePem = original.ExportPkcs8PrivateKeyPem();
        string expectedFingerprint = _service.GetPublicKeyFingerprint(publicPem);

        KeyPairResult result = _service.ImportKeyPair(privatePem, TestPassword);

        Assert.Equal(expectedFingerprint, result.PublicKeyFingerprint);
        Assert.NotEqual(privatePem, result.EncryptedPrivateKeyPem);

        using RSA imported = RSA.Create();
        imported.ImportFromEncryptedPem(result.EncryptedPrivateKeyPem, TestPassword);
        Assert.Equal(
            _service.GetPublicKeyFingerprint(imported.ExportSubjectPublicKeyInfoPem()),
            expectedFingerprint);
    }

    [Fact]
    public void ImportKeyPair_UnencryptedPkcs1PrivateKey_IsAccepted()
    {
        using RSA original = RSA.Create(2048);
        string pkcs1Pem = PemEncode("RSA PRIVATE KEY", original.ExportRSAPrivateKey());
        string expectedFingerprint = _service.GetPublicKeyFingerprint(original.ExportSubjectPublicKeyInfoPem());

        KeyPairResult result = _service.ImportKeyPair(pkcs1Pem, TestPassword);

        Assert.Equal(expectedFingerprint, result.PublicKeyFingerprint);
    }

    [Fact]
    public void ImportKeyPair_EncryptedPrivateKey_AcceptsCorrectPassword()
    {
        using RSA original = RSA.Create(2048);
        string encryptedPem = original.ExportEncryptedPkcs8PrivateKeyPem(
            "原始密码", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));
        string expectedFingerprint = _service.GetPublicKeyFingerprint(original.ExportSubjectPublicKeyInfoPem());

        KeyPairResult result = _service.ImportKeyPair(encryptedPem, "原始密码");

        Assert.Equal(expectedFingerprint, result.PublicKeyFingerprint);
        using RSA imported = RSA.Create();
        imported.ImportFromEncryptedPem(result.EncryptedPrivateKeyPem, "原始密码");
    }

    [Fact]
    public void ImportKeyPair_WrongPassword_ThrowsPrivateKeyInvalidOrPasswordWrong()
    {
        using RSA original = RSA.Create(2048);
        string encryptedPem = original.ExportEncryptedPkcs8PrivateKeyPem(
            "原始密码", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair(encryptedPem, "错误密码"));
        Assert.Equal("ErrorPrivateKeyInvalidOrPasswordWrong", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_PublicKeyPem_ThrowsPrivateKeyInvalidOrPasswordWrong()
    {
        using RSA original = RSA.Create(2048);
        string publicPem = original.ExportSubjectPublicKeyInfoPem();

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair(publicPem, TestPassword));
        Assert.Equal("ErrorPrivateKeyInvalidOrPasswordWrong", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_GarbagePem_ThrowsPrivateKeyInvalidOrPasswordWrong()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair("不是私钥内容", TestPassword));
        Assert.Equal("ErrorPrivateKeyInvalidOrPasswordWrong", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_SmallPrivateKey_ThrowsPrivateKeyTooSmall()
    {
        using RSA rsa = RSA.Create(1024);
        string privatePem = rsa.ExportPkcs8PrivateKeyPem();

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair(privatePem, TestPassword));
        Assert.Equal("ErrorPrivateKeyTooSmall", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_MissingPem_ThrowsPrivateKeyRequired()
    {
        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair(" ", TestPassword));
        Assert.Equal("ErrorPrivateKeyRequired", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_MissingPassword_ThrowsPasswordRequired()
    {
        using RSA rsa = RSA.Create(2048);

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ImportKeyPair(rsa.ExportPkcs8PrivateKeyPem(), " "));
        Assert.Equal("ErrorPasswordRequired", exception.ResourceKey);
    }

    [Fact]
    public void ChangePrivateKeyPassword_NewPasswordDecryptsOldPasswordFails()
    {
        using RSA original = RSA.Create(2048);
        string encryptedPem = original.ExportEncryptedPkcs8PrivateKeyPem(
            "旧密码", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));
        string expectedFingerprint = _service.GetPublicKeyFingerprint(original.ExportSubjectPublicKeyInfoPem());

        string newPem = _service.ChangePrivateKeyPassword(encryptedPem, "旧密码", "新密码");

        using RSA withNewPassword = RSA.Create();
        withNewPassword.ImportFromEncryptedPem(newPem, "新密码");
        Assert.Equal(
            _service.GetPublicKeyFingerprint(withNewPassword.ExportSubjectPublicKeyInfoPem()),
            expectedFingerprint);

        using RSA wrongPassword = RSA.Create();
        Assert.Throws<CryptographicException>(
            () => wrongPassword.ImportFromEncryptedPem(newPem, "旧密码"));
    }

    [Fact]
    public void ChangePrivateKeyPassword_WrongOldPassword_ThrowsPrivateKeyInvalidOrPasswordWrong()
    {
        using RSA original = RSA.Create(2048);
        string encryptedPem = original.ExportEncryptedPkcs8PrivateKeyPem(
            "旧密码", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ChangePrivateKeyPassword(encryptedPem, "错误旧密码", "新密码"));
        Assert.Equal("ErrorPrivateKeyInvalidOrPasswordWrong", exception.ResourceKey);
    }

    [Fact]
    public void ChangePrivateKeyPassword_EmptyNewPassword_ThrowsPasswordRequired()
    {
        using RSA original = RSA.Create(2048);
        string encryptedPem = original.ExportEncryptedPkcs8PrivateKeyPem(
            "旧密码", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));

        CryptoException exception = Assert.Throws<CryptoException>(
            () => _service.ChangePrivateKeyPassword(encryptedPem, "旧密码", " "));
        Assert.Equal("ErrorPasswordRequired", exception.ResourceKey);
    }

    [Fact]
    public void ImportKeyPair_DerivedPublicKey_IsDeterministic()
    {
        using RSA original = RSA.Create(2048);
        string privatePem = original.ExportPkcs8PrivateKeyPem();

        KeyPairResult first = _service.ImportKeyPair(privatePem, TestPassword);
        KeyPairResult second = _service.ImportKeyPair(privatePem, TestPassword);

        Assert.Equal(first.PublicKeyFingerprint, second.PublicKeyFingerprint);
        Assert.Equal(first.PublicKeyPem, second.PublicKeyPem);
    }

    private static string PemEncode(string label, ReadOnlySpan<byte> derBytes)
    {
        string base64 = Convert.ToBase64String(derBytes.ToArray());
        var builder = new System.Text.StringBuilder();
        builder.Append("-----BEGIN ").Append(label).AppendLine("-----");
        for (int offset = 0; offset < base64.Length; offset += 64)
        {
            int length = Math.Min(64, base64.Length - offset);
            builder.AppendLine(base64.Substring(offset, length));
        }

        builder.Append("-----END ").Append(label).AppendLine("-----");
        return builder.ToString();
    }
}
