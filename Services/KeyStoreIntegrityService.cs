using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace MessagesEncrypter.Services;

public sealed class KeyStoreIntegrityService
{
    private const int CredentialTypeGeneric = 1;
    private const int CredentialPersistenceLocalMachine = 2;
    private const int IntegrityKeyLength = 32;
    private const string IntegrityKeyTargetName = "MessagesEncrypter.KeyStoreIntegrityKey";
    private const string SignatureFileName = "keys.db.sig";

    private readonly string _folderPath;

    public KeyStoreIntegrityService(string folderPath)
    {
        _folderPath = folderPath;
    }

    public string SignaturePath => Path.Combine(_folderPath, SignatureFileName);

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
        catch (Exception ex) when (ex is FormatException or CryptographicException or Win32Exception)
        {
            throw new CryptoException("ErrorKeyStoreIntegrityInvalid", ex);
        }
    }

    public void SignFile(string filePath, bool resetIntegrityKey = false)
    {
        try
        {
            if (resetIntegrityKey)
            {
                DeleteIntegrityKey();
            }

            byte[] signature = ComputeSignature(filePath);
            File.WriteAllText(SignaturePath, Convert.ToBase64String(signature), Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or Win32Exception)
        {
            throw new CryptoException("ErrorKeyStoreIntegritySignFailed", ex);
        }
    }

    private byte[] ComputeSignature(string filePath)
    {
        byte[] key = GetOrCreateIntegrityKey();
        byte[] content = File.ReadAllBytes(filePath);
        return HMACSHA256.HashData(key, content);
    }

    private static byte[] GetOrCreateIntegrityKey()
    {
        byte[]? existingKey = ReadIntegrityKey();
        if (existingKey is not null)
        {
            return existingKey;
        }

        byte[] key = RandomNumberGenerator.GetBytes(IntegrityKeyLength);
        SaveIntegrityKey(key);
        return key;
    }

    private static byte[]? ReadIntegrityKey()
    {
        if (!CredReadW(IntegrityKeyTargetName, CredentialTypeGeneric, 0, out IntPtr credentialPointer))
        {
            return null;
        }

        try
        {
            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
            {
                return null;
            }

            string? protectedKeyText = Marshal.PtrToStringUni(
                credential.CredentialBlob,
                credential.CredentialBlobSize / sizeof(char));
            return string.IsNullOrWhiteSpace(protectedKeyText)
                ? null
                : Convert.FromBase64String(protectedKeyText);
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    private static void SaveIntegrityKey(byte[] key)
    {
        string keyText = Convert.ToBase64String(key);
        IntPtr passwordBlob = IntPtr.Zero;
        IntPtr userName = IntPtr.Zero;

        try
        {
            passwordBlob = Marshal.StringToCoTaskMemUni(keyText);
            userName = Marshal.StringToCoTaskMemUni(Environment.UserName);

            var credential = new NativeCredential
            {
                Type = CredentialTypeGeneric,
                TargetName = IntegrityKeyTargetName,
                CredentialBlobSize = Encoding.Unicode.GetByteCount(keyText),
                CredentialBlob = passwordBlob,
                Persist = CredentialPersistenceLocalMachine,
                UserName = userName
            };

            if (!CredWriteW(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            if (passwordBlob != IntPtr.Zero)
            {
                Marshal.ZeroFreeCoTaskMemUnicode(passwordBlob);
            }

            if (userName != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(userName);
            }
        }
    }

    private static void DeleteIntegrityKey()
    {
        if (!CredDeleteW(IntegrityKeyTargetName, CredentialTypeGeneric, 0))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != 1168)
            {
                throw new Win32Exception(error);
            }
        }
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWriteW(ref NativeCredential credential, int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredReadW(string targetName, int type, int flags, out IntPtr credentialPointer);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDeleteW(string targetName, int type, int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = false)]
    private static extern void CredFree(IntPtr credentialPointer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public IntPtr UserName;
    }
}
