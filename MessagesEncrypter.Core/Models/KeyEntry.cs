using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace MessagesEncrypter.Core.Models;

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

    [JsonIgnore]
    public string KeyType
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PublicKeyPem))
            {
                return "RSA";
            }

            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(PublicKeyPem);
                return $"RSA{rsa.KeySize}";
            }
            catch (Exception)
            {
                return "RSA";
            }
        }
    }

    [JsonIgnore]
    public string KeyTypeDisplay => KeyType;

    [JsonIgnore]
    public string FingerprintDisplay => Fingerprint;

    [JsonIgnore]
    public string DisplayName => $"{Alias} ({Fingerprint})";
}
