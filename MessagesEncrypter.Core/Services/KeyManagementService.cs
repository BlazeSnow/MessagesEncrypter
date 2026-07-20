using System;
using System.Linq;
using System.Security.Cryptography;

namespace MessagesEncrypter.Core.Services;

public sealed class KeyManagementService
{
    public KeyPairResult GenerateKeyPair(string password, int keySizeBits)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new CryptoException("ErrorPasswordRequired");
        }

        if (!CryptoConstants.SupportedRsaKeySizesBits.Contains(keySizeBits))
        {
            throw new CryptoException("ErrorRsaKeySizeUnsupported");
        }

        using RSA rsa = RSA.Create();
        rsa.KeySize = keySizeBits;
        string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        string privateKeyPem = rsa.ExportEncryptedPkcs8PrivateKeyPem(
            password,
            new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                CryptoConstants.PrivateKeyPbkdf2Iterations));

        return new KeyPairResult(publicKeyPem, privateKeyPem, GetPublicKeyFingerprint(publicKeyPem));
    }

    public KeyPairResult ImportKeyPair(string privateKeyPem, string password)
    {
        using RSA rsa = ImportPrivateKeyForStorage(privateKeyPem, password, out string encryptedPrivateKeyPem);
        string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        return new KeyPairResult(publicKeyPem, encryptedPrivateKeyPem, GetPublicKeyFingerprint(publicKeyPem));
    }

    public string ChangePrivateKeyPassword(string encryptedPrivateKeyPem, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new CryptoException("ErrorPasswordRequired");
        }

        using RSA rsa = ImportPrivateKey(encryptedPrivateKeyPem, oldPassword);
        return rsa.ExportEncryptedPkcs8PrivateKeyPem(
            newPassword,
            new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                CryptoConstants.PrivateKeyPbkdf2Iterations));
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
            if (rsa.KeySize < CryptoConstants.MinRsaKeySizeBits)
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
            if (rsa.KeySize < CryptoConstants.MinRsaKeySizeBits)
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

    private static RSA ImportPrivateKeyForStorage(string privateKeyPem, string password, out string encryptedPrivateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
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
            try
            {
                rsa.ImportFromEncryptedPem(privateKeyPem, password);
                encryptedPrivateKeyPem = privateKeyPem;
            }
            catch (CryptographicException)
            {
                rsa.ImportFromPem(privateKeyPem);
                encryptedPrivateKeyPem = rsa.ExportEncryptedPkcs8PrivateKeyPem(
                    password,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA256,
                        CryptoConstants.PrivateKeyPbkdf2Iterations));
            }

            if (rsa.KeySize < CryptoConstants.MinRsaKeySizeBits)
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
}
