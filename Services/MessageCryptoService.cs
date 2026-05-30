using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MessagesEncrypter.Models;

namespace MessagesEncrypter.Services;

public sealed class MessageCryptoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly KeyManagementService _keyManagementService;

    public MessageCryptoService(KeyManagementService keyManagementService)
    {
        _keyManagementService = keyManagementService;
    }

    public string EncryptToBase64Json(string plaintext, string publicKeyPem)
    {
        byte[] key = RandomNumberGenerator.GetBytes(CryptoConstants.AesKeySizeBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(CryptoConstants.AesGcmNonceSizeBytes);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[CryptoConstants.AesGcmTagSizeBytes];

        try
        {
            using RSA publicKey = _keyManagementService.ImportPublicKey(publicKeyPem);
            byte[] encryptedKey = publicKey.Encrypt(key, RSAEncryptionPadding.OaepSHA256);

            using var aesGcm = new AesGcm(key, CryptoConstants.AesGcmTagSizeBytes);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var package = new EncryptedMessagePackage(
                CryptoConstants.CurrentMessageVersion,
                Convert.ToBase64String(encryptedKey),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext));

            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(package, JsonOptions);
            return Convert.ToBase64String(jsonBytes);
        }
        catch (CryptoException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or JsonException)
        {
            throw new CryptoException("ErrorEncryptFailed", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    public string DecryptFromBase64Json(string armoredPackage, string encryptedPrivateKeyPem, string password)
    {
        if (string.IsNullOrWhiteSpace(armoredPackage))
        {
            throw new CryptoException("ErrorCipherTextRequired");
        }

        byte[]? key = null;
        byte[]? plaintextBytes = null;

        try
        {
            byte[] jsonBytes = Convert.FromBase64String(armoredPackage.Trim());
            EncryptedMessagePackage? package = JsonSerializer.Deserialize<EncryptedMessagePackage>(jsonBytes, JsonOptions);
            if (package is null || package.Ver != CryptoConstants.CurrentMessageVersion)
            {
                throw new CryptoException("ErrorUnsupportedMessageFormat");
            }

            byte[] encryptedKey = Convert.FromBase64String(package.EncryptedKey);
            byte[] nonce = Convert.FromBase64String(package.Nonce);
            byte[] tag = Convert.FromBase64String(package.Tag);
            byte[] ciphertext = Convert.FromBase64String(package.Ciphertext);

            if (nonce.Length != CryptoConstants.AesGcmNonceSizeBytes || tag.Length != CryptoConstants.AesGcmTagSizeBytes)
            {
                throw new CryptoException("ErrorUnsupportedMessageFormat");
            }

            using RSA privateKey = _keyManagementService.ImportPrivateKey(encryptedPrivateKeyPem, password);
            key = privateKey.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
            if (key.Length != CryptoConstants.AesKeySizeBytes)
            {
                throw new CryptoException("ErrorUnsupportedMessageFormat");
            }

            plaintextBytes = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(key, CryptoConstants.AesGcmTagSizeBytes);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (CryptoException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException or JsonException)
        {
            throw new CryptoException("ErrorDecryptFailed", ex);
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            if (plaintextBytes is not null)
            {
                CryptographicOperations.ZeroMemory(plaintextBytes);
            }
        }
    }
}
