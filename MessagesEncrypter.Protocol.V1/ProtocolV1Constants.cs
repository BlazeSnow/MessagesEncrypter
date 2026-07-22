namespace MessagesEncrypter.Protocol.V1;

internal static class ProtocolV1Constants
{
    public const int MessageVersion = 1;
    public const int AesKeySizeBytes = 32;
    public const int AesGcmNonceSizeBytes = 12;
    public const int AesGcmTagSizeBytes = 16;
    public const int MinimumRsaKeySizeBits = 2048;
}
