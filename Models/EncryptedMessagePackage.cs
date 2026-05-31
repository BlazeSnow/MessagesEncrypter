namespace MessagesEncrypter.Models;

public sealed record EncryptedMessagePackage(
    int Ver,
    string Ek,
    string Nonce,
    string Tag,
    string Ct);
