namespace MessagesEncrypter.Models;

public sealed record EncryptedMessagePackage(
    int Ver,
    string EncryptedKey,
    string Nonce,
    string Tag,
    string Ciphertext);
