using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MessagesEncrypter.Services;

public sealed class CredentialManagerService
{
    private const int CredentialTypeGeneric = 1;
    private const int CredentialPersistenceLocalMachine = 2;
    private const string TargetName = "MessagesEncrypter.PrivateKeyPassword";

    public void SavePrivateKeyPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new CryptoException("ErrorPasswordRequired");
        }

        IntPtr passwordBlob = IntPtr.Zero;
        IntPtr userName = IntPtr.Zero;

        try
        {
            passwordBlob = Marshal.StringToCoTaskMemUni(password);
            userName = Marshal.StringToCoTaskMemUni(Environment.UserName);

            var credential = new NativeCredential
            {
                Type = CredentialTypeGeneric,
                TargetName = TargetName,
                CredentialBlobSize = Encoding.Unicode.GetByteCount(password),
                CredentialBlob = passwordBlob,
                Persist = CredentialPersistenceLocalMachine,
                UserName = userName
            };

            if (!CredWriteW(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        catch (Exception ex) when (ex is Win32Exception or ArgumentException)
        {
            throw new CryptoException("ErrorCredentialSaveFailed", ex);
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

    public string GetPrivateKeyPassword()
    {
        string? password = ReadPrivateKeyPassword();
        if (password is null)
        {
            throw new CryptoException("ErrorCredentialPasswordMissing");
        }

        return password;
    }

    public bool HasPrivateKeyPassword()
    {
        return ReadPrivateKeyPassword() is not null;
    }

    public void DeletePrivateKeyPassword()
    {
        if (!CredDeleteW(TargetName, CredentialTypeGeneric, 0))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1168)
            {
                return;
            }

            throw new CryptoException("ErrorCredentialDeleteFailed", new Win32Exception(error));
        }
    }

    private static string? ReadPrivateKeyPassword()
    {
        if (!CredReadW(TargetName, CredentialTypeGeneric, 0, out IntPtr credentialPointer))
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

            string? password = Marshal.PtrToStringUni(
                credential.CredentialBlob,
                credential.CredentialBlobSize / sizeof(char));
            return string.IsNullOrEmpty(password) ? null : password;
        }
        finally
        {
            CredFree(credentialPointer);
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
