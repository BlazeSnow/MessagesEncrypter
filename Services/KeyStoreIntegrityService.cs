using System;
using System.IO;
using System.Runtime.InteropServices;
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
            byte[] encryptedKey = File.ReadAllBytes(IntegrityKeyPath);
            return UnprotectForCurrentUser(encryptedKey);
        }

        byte[] key = RandomNumberGenerator.GetBytes(IntegrityKeyLength);
        byte[] encryptedNewKey = ProtectForCurrentUser(key);
        File.WriteAllBytes(IntegrityKeyPath, encryptedNewKey);
        return key;
    }

    private static byte[] ProtectForCurrentUser(byte[] data)
    {
        DataBlob dataBlob = CreateBlob(data);
        DataBlob entropyBlob = CreateBlob(OptionalEntropy);
        try
        {
            if (!CryptProtectData(
                ref dataBlob,
                null,
                ref entropyBlob,
                IntPtr.Zero,
                IntPtr.Zero,
                CryptProtectUiForbidden,
                out DataBlob protectedBlob))
            {
                throw CreateDpapiException();
            }

            try
            {
                return CopyBlob(protectedBlob);
            }
            finally
            {
                FreeLocalBlob(protectedBlob);
            }
        }
        finally
        {
            FreeHGlobalBlob(dataBlob);
            FreeHGlobalBlob(entropyBlob);
        }
    }

    private static byte[] UnprotectForCurrentUser(byte[] data)
    {
        DataBlob dataBlob = CreateBlob(data);
        DataBlob entropyBlob = CreateBlob(OptionalEntropy);
        try
        {
            if (!CryptUnprotectData(
                ref dataBlob,
                IntPtr.Zero,
                ref entropyBlob,
                IntPtr.Zero,
                IntPtr.Zero,
                CryptProtectUiForbidden,
                out DataBlob unprotectedBlob))
            {
                throw CreateDpapiException();
            }

            try
            {
                return CopyBlob(unprotectedBlob);
            }
            finally
            {
                FreeLocalBlob(unprotectedBlob);
            }
        }
        finally
        {
            FreeHGlobalBlob(dataBlob);
            FreeHGlobalBlob(entropyBlob);
        }
    }

    private static DataBlob CreateBlob(byte[] data)
    {
        IntPtr dataPointer = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, dataPointer, data.Length);
        return new DataBlob(data.Length, dataPointer);
    }

    private static byte[] CopyBlob(DataBlob blob)
    {
        byte[] data = new byte[blob.Size];
        Marshal.Copy(blob.Data, data, 0, blob.Size);
        return data;
    }

    private static void FreeHGlobalBlob(DataBlob blob)
    {
        if (blob.Data != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(blob.Data);
        }
    }

    private static void FreeLocalBlob(DataBlob blob)
    {
        if (blob.Data != IntPtr.Zero)
        {
            _ = LocalFree(blob.Data);
        }
    }

    private static CryptographicException CreateDpapiException()
    {
        return new CryptographicException(Marshal.GetLastWin32Error());
    }

    private const int CryptProtectUiForbidden = 0x1;

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string? dataDescription,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr dataDescription,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct DataBlob
    {
        public DataBlob(int size, IntPtr data)
        {
            Size = size;
            Data = data;
        }

        public readonly int Size;

        public readonly IntPtr Data;
    }
}
