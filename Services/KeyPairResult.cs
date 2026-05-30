namespace MessagesEncrypter.Services;

public sealed record KeyPairResult(
    string PublicKeyPem,
    string EncryptedPrivateKeyPem,
    string PublicKeyFingerprint);
