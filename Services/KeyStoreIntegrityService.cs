using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MessagesEncrypter.Services;

public sealed class KeyStoreIntegrityService
{
    private const string IntegrityKeyFileName = "keys.integrity";
    private const string SignatureFileName = "keys.db.sig";
    private const int IntegrityKeyLength = 32;

    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("MessagesEncrypter.KeyStoreIntegrity.v1");

    private readonly string _folderPath;

    public KeyStoreIntegrityService(string folderPath)
    {
        _folderPath = folderPath;
    }

    private string IntegrityKeyPath => Path.Combine(_folderPath, IntegrityKeyFileName);

    public string SignaturePath => Path.Combine(_folderPath, SignatureFileName);

    public bool HasSignature => File.Exists(SignaturePath);

    public void VerifyFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            if (!File.Exists(SignaturePath))
            {
                throw new CryptoException("ErrorKeyStoreIntegrityMissing");
            }

            byte[] expectedSignature = Convert.FromBase64String(File.ReadAllText(SignaturePath, Encoding.UTF8));
            byte[] actualSignature = ComputeSignature(filePath);
            if (expectedSignature.Length != actualSignature.Length ||
                !CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
            {
                throw new CryptoException("ErrorKeyStoreIntegrityInvalid");
            }
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new CryptoException("ErrorKeyStoreIntegrityInvalid", ex);
        }
    }

    public void SignFile(string filePath)
    {
        byte[] signature = ComputeSignature(filePath);
        File.WriteAllText(SignaturePath, Convert.ToBase64String(signature), Encoding.UTF8);
    }

    private byte[] ComputeSignature(string filePath)
    {
        byte[] key = GetOrCreateIntegrityKey();
        byte[] content = File.ReadAllBytes(filePath);
        return HMACSHA256.HashData(key, content);
    }

    private byte[] GetOrCreateIntegrityKey()
    {
        if (File.Exists(IntegrityKeyPath))
        {
            byte[] protectedKey = File.ReadAllBytes(IntegrityKeyPath);
            return ProtectedData.Unprotect(protectedKey, OptionalEntropy, DataProtectionScope.CurrentUser);
        }

        byte[] key = RandomNumberGenerator.GetBytes(IntegrityKeyLength);
        byte[] protectedKey = ProtectedData.Protect(key, OptionalEntropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(IntegrityKeyPath, protectedKey);
        return key;
    }
}
