using System;
using System.Security.Cryptography;

namespace MessagesEncrypter.Services;

public sealed class KeyManagementService
{
    public KeyPairResult GenerateKeyPair(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new CryptoException("ErrorPasswordRequired");
        }

        using RSA rsa = RSA.Create(CryptoConstants.RsaKeySizeBits);
        string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        string privateKeyPem = rsa.ExportEncryptedPkcs8PrivateKeyPem(
            password,
            new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                CryptoConstants.PrivateKeyPbkdf2Iterations));

        return new KeyPairResult(publicKeyPem, privateKeyPem, GetPublicKeyFingerprint(publicKeyPem));
    }

    public RSA ImportPublicKey(string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            throw new CryptoException("ErrorPublicKeyRequired");
        }

        try
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            if (rsa.KeySize < CryptoConstants.RsaKeySizeBits)
            {
                rsa.Dispose();
                throw new CryptoException("ErrorPublicKeyTooSmall");
            }

            return rsa;
        }
        catch (CryptoException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new CryptoException("ErrorPublicKeyInvalid", ex);
        }
    }

    public RSA ImportPrivateKey(string encryptedPrivateKeyPem, string password)
    {
        if (string.IsNullOrWhiteSpace(encryptedPrivateKeyPem))
        {
            throw new CryptoException("ErrorPrivateKeyRequired");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new CryptoException("ErrorPasswordRequired");
        }

        try
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromEncryptedPem(encryptedPrivateKeyPem, password);
            if (rsa.KeySize < CryptoConstants.RsaKeySizeBits)
            {
                rsa.Dispose();
                throw new CryptoException("ErrorPrivateKeyTooSmall");
            }

            return rsa;
        }
        catch (CryptoException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new CryptoException("ErrorPrivateKeyInvalidOrPasswordWrong", ex);
        }
    }

    public string GetPublicKeyFingerprint(string publicKeyPem)
    {
        using RSA rsa = ImportPublicKey(publicKeyPem);
        byte[] spki = rsa.ExportSubjectPublicKeyInfo();
        byte[] hash = SHA256.HashData(spki);
        byte[] displayedHash = hash[..CryptoConstants.FingerprintBytesToDisplay];
        return Convert.ToHexString(displayedHash);
    }
}
