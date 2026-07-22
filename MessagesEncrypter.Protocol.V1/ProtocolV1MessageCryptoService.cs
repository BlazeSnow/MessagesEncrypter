using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MessagesEncrypter.Protocol.V1;

public sealed class ProtocolV1MessageCryptoService
{
    public string EncryptToBase64Json(string plaintext, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ProtocolV1Exception("ErrorPlainTextRequired");
        }

        byte[] key = RandomNumberGenerator.GetBytes(ProtocolV1Constants.AesKeySizeBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(ProtocolV1Constants.AesGcmNonceSizeBytes);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[ProtocolV1Constants.AesGcmTagSizeBytes];

        try
        {
            using RSA publicKey = ImportPublicKey(publicKeyPem);
            byte[] encryptedKey = publicKey.Encrypt(key, RSAEncryptionPadding.OaepSHA256);

            using var aesGcm = new AesGcm(key, ProtocolV1Constants.AesGcmTagSizeBytes);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var package = new EncryptedMessagePackage(
                ProtocolV1Constants.MessageVersion,
                Convert.ToBase64String(encryptedKey),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext));

            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
                package,
                ProtocolV1JsonSerializerContext.Default.EncryptedMessagePackage);
            return Convert.ToBase64String(jsonBytes);
        }
        catch (ProtocolV1Exception)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or JsonException)
        {
            throw new ProtocolV1Exception("ErrorEncryptFailed", ex);
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
            throw new ProtocolV1Exception("ErrorCipherTextRequired");
        }

        byte[]? key = null;
        byte[]? plaintextBytes = null;

        try
        {
            byte[] jsonBytes = Convert.FromBase64String(armoredPackage.Trim());
            EncryptedMessagePackage? package = JsonSerializer.Deserialize(
                jsonBytes,
                ProtocolV1JsonSerializerContext.Default.EncryptedMessagePackage);
            if (package is null || package.Ver != ProtocolV1Constants.MessageVersion)
            {
                throw new ProtocolV1Exception("ErrorUnsupportedMessageFormat");
            }

            if (string.IsNullOrWhiteSpace(package.Ek)
                || string.IsNullOrWhiteSpace(package.Nonce)
                || string.IsNullOrWhiteSpace(package.Tag)
                || string.IsNullOrWhiteSpace(package.Ct))
            {
                throw new ProtocolV1Exception("ErrorUnsupportedMessageFormat");
            }

            byte[] encryptedKey = Convert.FromBase64String(package.Ek);
            byte[] nonce = Convert.FromBase64String(package.Nonce);
            byte[] tag = Convert.FromBase64String(package.Tag);
            byte[] ciphertext = Convert.FromBase64String(package.Ct);

            if (nonce.Length != ProtocolV1Constants.AesGcmNonceSizeBytes
                || tag.Length != ProtocolV1Constants.AesGcmTagSizeBytes)
            {
                throw new ProtocolV1Exception("ErrorUnsupportedMessageFormat");
            }

            using RSA privateKey = ImportPrivateKey(encryptedPrivateKeyPem, password);
            key = privateKey.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
            if (key.Length != ProtocolV1Constants.AesKeySizeBytes)
            {
                throw new ProtocolV1Exception("ErrorUnsupportedMessageFormat");
            }

            plaintextBytes = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(key, ProtocolV1Constants.AesGcmTagSizeBytes);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (ProtocolV1Exception)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException or JsonException)
        {
            throw new ProtocolV1Exception("ErrorDecryptFailed", ex);
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

    private static RSA ImportPublicKey(string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            throw new ProtocolV1Exception("ErrorPublicKeyRequired");
        }

        try
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            if (rsa.KeySize < ProtocolV1Constants.MinimumRsaKeySizeBits)
            {
                rsa.Dispose();
                throw new ProtocolV1Exception("ErrorPublicKeyTooSmall");
            }

            return rsa;
        }
        catch (ProtocolV1Exception)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new ProtocolV1Exception("ErrorPublicKeyInvalid", ex);
        }
    }

    private static RSA ImportPrivateKey(string encryptedPrivateKeyPem, string password)
    {
        if (string.IsNullOrWhiteSpace(encryptedPrivateKeyPem))
        {
            throw new ProtocolV1Exception("ErrorPrivateKeyRequired");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ProtocolV1Exception("ErrorPasswordRequired");
        }

        try
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromEncryptedPem(encryptedPrivateKeyPem, password);
            if (rsa.KeySize < ProtocolV1Constants.MinimumRsaKeySizeBits)
            {
                rsa.Dispose();
                throw new ProtocolV1Exception("ErrorPrivateKeyTooSmall");
            }

            return rsa;
        }
        catch (ProtocolV1Exception)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new ProtocolV1Exception("ErrorPrivateKeyInvalidOrPasswordWrong", ex);
        }
    }
}
