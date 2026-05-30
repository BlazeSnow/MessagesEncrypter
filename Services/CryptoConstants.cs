namespace MessagesEncrypter.Services;

internal static class CryptoConstants
{
    public const int CurrentMessageVersion = 1;
    public const int AesKeySizeBytes = 32;
    public const int AesGcmNonceSizeBytes = 12;
    public const int AesGcmTagSizeBytes = 16;
    public const int RsaKeySizeBits = 4096;
    public const int PrivateKeyPbkdf2Iterations = 600_000;
    public const int FingerprintBytesToDisplay = 16;
}
