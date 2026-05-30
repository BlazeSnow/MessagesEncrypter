namespace MessagesEncrypter.Models;

public sealed class KeyEntry
{
    public KeyEntry(string alias, string fingerprint, string? publicKeyPem, string? encryptedPrivateKeyPem)
    {
        Alias = alias;
        Fingerprint = fingerprint;
        PublicKeyPem = publicKeyPem;
        EncryptedPrivateKeyPem = encryptedPrivateKeyPem;
    }

    public string Alias { get; }

    public string Fingerprint { get; }

    public string? PublicKeyPem { get; }

    public string? EncryptedPrivateKeyPem { get; }

    public string DisplayName => $"{Alias} ({Fingerprint})";
}
