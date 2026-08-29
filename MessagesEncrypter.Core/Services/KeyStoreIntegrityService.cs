using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MessagesEncrypter.Core.Services;

public sealed class KeyStoreIntegrityService
{
    private const int CredentialTypeGeneric = 1;
    private const int CredentialPersistenceLocalMachine = 2;
    private const int IntegrityKeyLength = 32;
    private const string IntegrityKeyTargetName = "MessagesEncrypter.KeyStoreIntegrityKey";
    private const string SignatureFileName = "keys.db.sig";

    private readonly string _folderPath;
    private readonly string _integrityKeyTargetName;

    public KeyStoreIntegrityService(string folderPath, string? integrityKeyTargetName = null)
    {
        _folderPath = folderPath;
        _integrityKeyTargetName = integrityKeyTargetName ?? IntegrityKeyTargetName;
    }

    public string SignaturePath => Path.Combine(_folderPath, SignatureFileName);

    public string? GetIntegrityErrorResourceKey(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            if (!File.Exists(SignaturePath))
            {
                return "ErrorKeyStoreIntegrityMissing";
            }

            byte[] expectedSignature = Convert.FromBase64String(File.ReadAllText(SignaturePath, Encoding.UTF8));
            byte[] actualSignature = ComputeSignature(filePath);
            if (expectedSignature.Length != actualSignature.Length ||
                !CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
            {
                return "ErrorKeyStoreIntegrityInvalid";
            }

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or CryptographicException or Win32Exception)
        {
            return "ErrorKeyStoreIntegrityInvalid";
        }
    }

    public void VerifyFile(string filePath)
    {
        string? errorResourceKey = GetIntegrityErrorResourceKey(filePath);
        if (errorResourceKey is not null)
        {
            throw new CryptoException(errorResourceKey);
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or CryptographicException or Win32Exception)
        {
            throw new CryptoException("ErrorKeyStoreIntegritySignFailed", ex);
        }
    }

    private byte[] ComputeSignature(string filePath)
    {
        byte[] key = GetOrCreateIntegrityKey();
        using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        return HMACSHA256.HashData(key, stream);
    }

    private byte[] GetOrCreateIntegrityKey()
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

    private byte[]? ReadIntegrityKey()
    {
        // 凭据管理器在并发调用时可能返回瞬时错误（如 ERROR_NO_SUCH_LOGON_SESSION）。
        // 此时不能把读取失败当作密钥不存在而轮换密钥，否则签名链会被永久破坏，
        // 导致下次启动误报密钥库被篡改；因此仅对 ERROR_NOT_FOUND 视为不存在，其余重试后报错。
        for (int attempt = 1; ; attempt++)
        {
            if (CredReadW(_integrityKeyTargetName, CredentialTypeGeneric, 0, out IntPtr credentialPointer))
            {
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

            int error = Marshal.GetLastWin32Error();
            if (error == 1168)
            {
                return null;
            }

            if (attempt >= 3)
            {
                throw new Win32Exception(error);
            }

            Thread.Sleep(50);
        }
    }

    private void SaveIntegrityKey(byte[] key)
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
                TargetName = _integrityKeyTargetName,
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

    internal void DeleteIntegrityKey()
    {
        if (!CredDeleteW(_integrityKeyTargetName, CredentialTypeGeneric, 0))
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
