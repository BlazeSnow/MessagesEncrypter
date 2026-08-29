using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MessagesEncrypter.Protocol.V1;
using Xunit;

namespace MessagesEncrypter.Tests;

/// <summary>
/// 消息协议 V1 加解密测试，覆盖 DEVELOPMENT.md「测试重点」中的协议相关场景。
/// </summary>
public sealed class ProtocolV1MessageCryptoServiceTests
{
    private const string TestPassword = "test-password-密码";

    private readonly ProtocolV1MessageCryptoService _service = new();

    [Fact]
    public void EncryptThenDecrypt_ShortMessage_RoundTrips()
    {
        AssertRoundTrip("你好，世界");
    }

    [Fact]
    public void EncryptThenDecrypt_LongMessage_RoundTrips()
    {
        AssertRoundTrip(new string('A', 100_000));
    }

    [Fact]
    public void EncryptThenDecrypt_ComplexUnicode_RoundTrips()
    {
        string message = "中文消息 🌍🚀🎉 Emoji、日本語テキスト、한국어 텍스트、مرحبا بالعالم、Ωμέγα ↔️ 混合内容\r\n换行\t制表符";
        AssertRoundTrip(message);
    }

    [Fact]
    public void Encrypt_NullPlaintext_ThrowsPlainTextRequired()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.EncryptToBase64Json(null!, keys.PublicKeyPem));
        Assert.Equal("ErrorPlainTextRequired", exception.ResourceKey);
    }

    [Fact]
    public void Encrypt_WhitespacePlaintext_ThrowsPlainTextRequired()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.EncryptToBase64Json("   \r\n\t ", keys.PublicKeyPem));
        Assert.Equal("ErrorPlainTextRequired", exception.ResourceKey);
    }

    [Fact]
    public void Encrypt_MissingPublicKey_ThrowsPublicKeyRequired()
    {
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.EncryptToBase64Json("消息", "  "));
        Assert.Equal("ErrorPublicKeyRequired", exception.ResourceKey);
    }

    [Fact]
    public void Encrypt_GarbagePublicKey_ThrowsPublicKeyInvalid()
    {
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.EncryptToBase64Json("消息", "这不是 PEM"));
        Assert.Equal("ErrorPublicKeyInvalid", exception.ResourceKey);
    }

    [Fact]
    public void Encrypt_SmallPublicKey_ThrowsPublicKeyTooSmall()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(1024);
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.EncryptToBase64Json("消息", keys.PublicKeyPem));
        Assert.Equal("ErrorPublicKeyTooSmall", exception.ResourceKey);
    }

    [Fact]
    public void EncryptOutput_IsBase64WrappedJson_WithExpectedWireFields()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("格式校验", keys.PublicKeyPem);

        byte[] jsonBytes = Convert.FromBase64String(armored);
        using var document = JsonDocument.Parse(jsonBytes);
        JsonElement root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(5, root.EnumerateObject().Count());
        Assert.Equal(1, root.GetProperty("ver").GetInt32());
        Assert.Equal(12, Convert.FromBase64String(root.GetProperty("nonce").GetString()!).Length);
        Assert.Equal(16, Convert.FromBase64String(root.GetProperty("tag").GetString()!).Length);
        Assert.Equal(32, keys.DecryptSessionKey(root.GetProperty("ek").GetString()!).Length);
        Assert.False(string.IsNullOrEmpty(root.GetProperty("ct").GetString()));
    }

    [Fact]
    public void Decrypt_MissingVersion_ThrowsUnsupportedMessageFormat()
    {
        AssertDecryptPackageThrows("""{"ek":"AAA","nonce":"AAA","tag":"AAA","ct":"AAA"}""", "ErrorUnsupportedMessageFormat");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void Decrypt_UnsupportedVersion_ThrowsUnsupportedMessageFormat(int version)
    {
        AssertDecryptPackageThrows($$"""{"ver":{{version}},"ek":"AAA","nonce":"AAA","tag":"AAA","ct":"AAA"}""", "ErrorUnsupportedMessageFormat");
    }

    [Theory]
    [InlineData("ek")]
    [InlineData("nonce")]
    [InlineData("tag")]
    [InlineData("ct")]
    public void Decrypt_MissingRequiredField_ThrowsUnsupportedMessageFormat(string missingField)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("缺失字段", keys.PublicKeyPem);
        string modified = RemoveField(armored, missingField);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorUnsupportedMessageFormat", exception.ResourceKey);
    }

    [Theory]
    [InlineData("ek")]
    [InlineData("nonce")]
    [InlineData("tag")]
    [InlineData("ct")]
    public void Decrypt_NullRequiredField_ThrowsUnsupportedMessageFormat(string nullField)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("空字段", keys.PublicKeyPem);
        string modified = ReplaceFieldValue(armored, nullField, null);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorUnsupportedMessageFormat", exception.ResourceKey);
    }

    [Theory]
    [InlineData("ek")]
    [InlineData("nonce")]
    [InlineData("tag")]
    [InlineData("ct")]
    public void Decrypt_InvalidBase64Field_ThrowsDecryptFailed(string brokenField)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("非法 Base64", keys.PublicKeyPem);
        string modified = ReplaceFieldValue(armored, brokenField, "!!!不是 Base64!!!");

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorDecryptFailed", exception.ResourceKey);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(0)]
    public void Decrypt_WrongNonceLength_ThrowsUnsupportedMessageFormat(int nonceLength)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("nonce 长度异常", keys.PublicKeyPem);
        string modified = ReplaceFieldValue(
            armored, "nonce", Convert.ToBase64String(new byte[nonceLength]));

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorUnsupportedMessageFormat", exception.ResourceKey);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(0)]
    public void Decrypt_WrongTagLength_ThrowsUnsupportedMessageFormat(int tagLength)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("tag 长度异常", keys.PublicKeyPem);
        string modified = ReplaceFieldValue(
            armored, "tag", Convert.ToBase64String(new byte[tagLength]));

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorUnsupportedMessageFormat", exception.ResourceKey);
    }

    [Theory]
    [InlineData("ek")]
    [InlineData("nonce")]
    [InlineData("tag")]
    [InlineData("ct")]
    public void Decrypt_TamperedField_ThrowsDecryptFailed(string tamperedField)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("篡改测试", keys.PublicKeyPem);
        string modified = TamperFieldValue(armored, tamperedField);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorDecryptFailed", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_UnknownExtraFields_AreIgnored()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("未知字段", keys.PublicKeyPem);
        string withExtraFields = AddExtraFields(armored);

        string plaintext = _service.DecryptFromBase64Json(
            withExtraFields, keys.EncryptedPrivateKeyPem, TestPassword);
        Assert.Equal("未知字段", plaintext);
    }

    [Fact]
    public void Decrypt_WrongPrivateKey_ThrowsDecryptFailed()
    {
        using RsaKeyPair senderKeys = RsaKeyPair.Create(2048);
        using RsaKeyPair otherKeys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("密钥不符", senderKeys.PublicKeyPem);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, otherKeys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorDecryptFailed", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_WrongPassword_ThrowsPrivateKeyInvalidOrPasswordWrong()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("密码错误", keys.PublicKeyPem);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, keys.EncryptedPrivateKeyPem, "错误密码"));
        Assert.Equal("ErrorPrivateKeyInvalidOrPasswordWrong", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_MissingPrivateKey_ThrowsPrivateKeyRequired()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("缺少私钥", keys.PublicKeyPem);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, " ", TestPassword));
        Assert.Equal("ErrorPrivateKeyRequired", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_MissingPassword_ThrowsPasswordRequired()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("缺少密码", keys.PublicKeyPem);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, keys.EncryptedPrivateKeyPem, " "));
        Assert.Equal("ErrorPasswordRequired", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_SmallPrivateKey_ThrowsPrivateKeyTooSmall()
    {
        using RsaKeyPair smallKeys = RsaKeyPair.Create(1024);
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("私钥过小", keys.PublicKeyPem);

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, smallKeys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorPrivateKeyTooSmall", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_EmptyArmoredPackage_ThrowsCipherTextRequired()
    {
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json("   ", "-----BEGIN PUBLIC KEY-----", TestPassword));
        Assert.Equal("ErrorCipherTextRequired", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_NotBase64_ThrowsDecryptFailed()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json("这不是 Base64！！", keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorDecryptFailed", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_Base64OfNonJson_ThrowsDecryptFailed()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = Convert.ToBase64String(Encoding.UTF8.GetBytes("纯文本，不是 JSON"));

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorDecryptFailed", exception.ResourceKey);
    }

    [Fact]
    public void Decrypt_EmptyCiphertextField_ThrowsUnsupportedMessageFormat()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = _service.EncryptToBase64Json("空密文", keys.PublicKeyPem);
        string modified = ReplaceFieldValue(armored, "ct", "");

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(modified, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal("ErrorUnsupportedMessageFormat", exception.ResourceKey);
    }

    [Fact]
    public void Encrypt_EachCallUsesFreshSessionKeyAndNonce()
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string first = _service.EncryptToBase64Json("相同明文", keys.PublicKeyPem);
        string second = _service.EncryptToBase64Json("相同明文", keys.PublicKeyPem);

        Assert.NotEqual(first, second);

        byte[] firstNonce = Convert.FromBase64String(GetField(first, "nonce"));
        byte[] secondNonce = Convert.FromBase64String(GetField(second, "nonce"));
        Assert.NotEqual(firstNonce, secondNonce);

        Assert.NotEqual(
            GetField(first, "ct"),
            GetField(second, "ct"));
    }

    private void AssertRoundTrip(string message)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(4096);
        string armored = _service.EncryptToBase64Json(message, keys.PublicKeyPem);
        string decrypted = _service.DecryptFromBase64Json(
            armored, keys.EncryptedPrivateKeyPem, TestPassword);
        Assert.Equal(message, decrypted);
    }

    private void AssertDecryptPackageThrows(string packageJson, string expectedResourceKey)
    {
        using RsaKeyPair keys = RsaKeyPair.Create(2048);
        string armored = Convert.ToBase64String(Encoding.UTF8.GetBytes(packageJson));

        ProtocolV1Exception exception = Assert.Throws<ProtocolV1Exception>(
            () => _service.DecryptFromBase64Json(armored, keys.EncryptedPrivateKeyPem, TestPassword));
        Assert.Equal(expectedResourceKey, exception.ResourceKey);
    }

    private static string GetField(string armored, string fieldName)
    {
        byte[] jsonBytes = Convert.FromBase64String(armored);
        using var document = JsonDocument.Parse(jsonBytes);
        return document.RootElement.GetProperty(fieldName).GetString()!;
    }

    private static string RewritePackage(string armored, Func<Dictionary<string, object?>, Dictionary<string, object?>> rewrite)
    {
        byte[] jsonBytes = Convert.FromBase64String(armored);
        using JsonDocument document = JsonDocument.Parse(jsonBytes);
        var fields = new Dictionary<string, object?>();
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            fields[property.Name] = property.Value.ValueKind == JsonValueKind.Null
                ? null
                : property.Value.Clone();
        }

        Dictionary<string, object?> rewritten = rewrite(fields);
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(rewritten));
    }

    private static string ReplaceFieldValue(string armored, string fieldName, object? value)
    {
        return RewritePackage(armored, fields =>
        {
            fields[fieldName] = value;
            return fields;
        });
    }

    private static string RemoveField(string armored, string fieldName)
    {
        return RewritePackage(armored, fields =>
        {
            fields.Remove(fieldName);
            return fields;
        });
    }

    private static string AddExtraFields(string armored)
    {
        return RewritePackage(armored, fields =>
        {
            fields["alg"] = "RSA-OAEP-SHA256";
            fields["future"] = "未知扩展";
            return fields;
        });
    }

    private static string TamperFieldValue(string armored, string fieldName)
    {
        return RewritePackage(armored, fields =>
        {
            byte[] original = Convert.FromBase64String(((JsonElement)fields[fieldName]!).GetString()!);
            byte[] tampered = (byte[])original.Clone();
            tampered[0] ^= 0xFF;
            fields[fieldName] = Convert.ToBase64String(tampered);
            return fields;
        });
    }

    /// <summary>测试用 RSA 密钥对，私钥以加密 PKCS#8 PEM 保存。</summary>
    private sealed class RsaKeyPair : IDisposable
    {
        private readonly RSA _rsa;

        private RsaKeyPair(RSA rsa)
        {
            _rsa = rsa;
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            EncryptedPrivateKeyPem = rsa.ExportEncryptedPkcs8PrivateKeyPem(
                TestPassword,
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));
        }

        public string PublicKeyPem { get; }

        public string EncryptedPrivateKeyPem { get; }

        public static RsaKeyPair Create(int keySizeBits) => new(RSA.Create(keySizeBits));

        public byte[] DecryptSessionKey(string encryptedKeyBase64)
        {
            return _rsa.Decrypt(
                Convert.FromBase64String(encryptedKeyBase64),
                RSAEncryptionPadding.OaepSHA256);
        }

        public void Dispose() => _rsa.Dispose();
    }
}
