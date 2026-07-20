namespace MessagesEncrypter.Core.Services;

public static class CryptoConstants
{
    public const int CurrentMessageVersion = 1;
    public const int AesKeySizeBytes = 32;
    public const int AesGcmNonceSizeBytes = 12;
    public const int AesGcmTagSizeBytes = 16;
    public const int MinRsaKeySizeBits = 2048;
    public const int DefaultRsaKeySizeBits = 4096;
    public const int PrivateKeyPbkdf2Iterations = 600_000;
    public const int FingerprintBytesToDisplay = 16;

    public static readonly int[] SupportedRsaKeySizesBits = [2048, 3072, 4096, 8192];
}
